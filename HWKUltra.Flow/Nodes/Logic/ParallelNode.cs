using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Parallel node - runs N sub-flows concurrently.
    /// Supports flexible wait modes: All (wait for all), Any (wait for first success),
    /// None (fire-and-forget, but still tracked).
    /// All sub-flows share the same SharedFlowContext for cross-flow communication.
    /// </summary>
    public class ParallelNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Parallel";
        public override string NodeType => "Parallel";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "FlowPaths", DisplayName = "Flow Paths", Type = "string", Required = true, Description = "Comma-separated paths to flow definition JSON files" },
            new FlowParameter { Name = "WaitMode", DisplayName = "Wait Mode", Type = "string", Required = false, DefaultValue = "All", Description = "All = wait for all, Any = wait for first success, None = fire-and-forget" },
            new FlowParameter { Name = "TimeoutMs", DisplayName = "Timeout (ms)", Type = "int", Required = false, DefaultValue = 0, Description = "Overall timeout in ms (0 = no timeout)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "CompletedCount", DisplayName = "Completed Count", Type = "int", Description = "Number of sub-flows that completed successfully" },
            new FlowParameter { Name = "TotalCount", DisplayName = "Total Count", Type = "int", Description = "Total number of sub-flows launched" },
            new FlowParameter { Name = "AllSuccess", DisplayName = "All Success", Type = "bool", Description = "Whether all sub-flows completed successfully" },
            new FlowParameter { Name = "Errors", DisplayName = "Errors", Type = "string", Description = "Comma-separated error messages from failed sub-flows" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var flowPathsRaw = context.GetNodeInput<string>(Id, "FlowPaths") ?? "";
                var waitMode = context.GetNodeInput<string>(Id, "WaitMode") ?? "All";
                var timeoutMs = context.GetNodeInput<int>(Id, "TimeoutMs");

                if (string.IsNullOrEmpty(flowPathsRaw))
                    return FlowResult.Fail("FlowPaths is required");

                if (context.NodeFactory == null)
                    return FlowResult.Fail("NodeFactory is not set in FlowContext. Required for ParallelNode.");

                var flowPaths = flowPathsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (flowPaths.Length == 0)
                    return FlowResult.Fail("No flow paths specified");

                // Ensure SharedFlowContext exists (auto-create if needed)
                context.SharedContext ??= new SharedFlowContext();

                Console.WriteLine($"[Parallel] Launching {flowPaths.Length} sub-flows, WaitMode={waitMode}");

                // Create cancellation for timeout
                using var timeoutCts = timeoutMs > 0
                    ? new CancellationTokenSource(timeoutMs)
                    : new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, timeoutCts.Token);

                // Launch all sub-flows
                var tasks = new List<Task<SubFlowResult>>();
                for (int i = 0; i < flowPaths.Length; i++)
                {
                    var path = flowPaths[i];
                    var index = i;
                    tasks.Add(Task.Run(() => ExecuteSubFlowAsync(path, index, context, linkedCts.Token), linkedCts.Token));
                }

                // Wait based on mode
                var results = await WaitByModeAsync(tasks, waitMode, linkedCts.Token);

                // Aggregate results
                int completedCount = results.Count(r => r.Success);
                int totalCount = flowPaths.Length;
                bool allSuccess = completedCount == totalCount;
                var errors = string.Join(", ", results.Where(r => !r.Success).Select(r => r.Error));

                context.SetNodeOutput(Id, "CompletedCount", completedCount);
                context.SetNodeOutput(Id, "TotalCount", totalCount);
                context.SetNodeOutput(Id, "AllSuccess", allSuccess);
                context.SetNodeOutput(Id, "Errors", errors);

                Console.WriteLine($"[Parallel] Done: {completedCount}/{totalCount} succeeded");

                if (waitMode == "All" && !allSuccess)
                    return FlowResult.Fail($"Parallel: {totalCount - completedCount} sub-flow(s) failed: {errors}");

                return FlowResult.Ok();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Parallel execution failed: {ex.Message}");
            }
        }

        private async Task<SubFlowResult> ExecuteSubFlowAsync(string flowPath, int index, FlowContext parentContext, CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(flowPath))
                    return new SubFlowResult { Index = index, Success = false, Error = $"File not found: {flowPath}" };

                var definition = FlowSerializer.LoadFromFile(flowPath);
                if (definition == null)
                    return new SubFlowResult { Index = index, Success = false, Error = $"Failed to load: {flowPath}" };

                var engine = new FlowEngine(definition);

                // Register nodes
                foreach (var nodeDef in definition.Nodes)
                {
                    var node = parentContext.NodeFactory!.CreateNode(nodeDef.Type, nodeDef.Properties);
                    node.Id = nodeDef.Id;
                    node.Name = nodeDef.Name;
                    node.Description = nodeDef.Description;
                    engine.RegisterNode(node);
                }

                // Create isolated child context with shared context bridge
                var childContext = new FlowContext
                {
                    SharedContext = parentContext.SharedContext,
                    NodeFactory = parentContext.NodeFactory
                };

                // Inject node properties
                foreach (var nodeDef in definition.Nodes)
                {
                    foreach (var prop in nodeDef.Properties)
                    {
                        childContext.Variables[$"{nodeDef.Id}:{prop.Key}"] = prop.Value;
                    }
                }

                Console.WriteLine($"[Parallel#{index}] Starting: {definition.Name}");
                var result = await engine.ExecuteAsync(childContext, cancellationToken);
                Console.WriteLine($"[Parallel#{index}] Completed: {definition.Name}, Success={result.Success}");

                return new SubFlowResult { Index = index, Success = result.Success, Error = result.ErrorMessage };
            }
            catch (OperationCanceledException)
            {
                return new SubFlowResult { Index = index, Success = false, Error = "Cancelled" };
            }
            catch (Exception ex)
            {
                return new SubFlowResult { Index = index, Success = false, Error = ex.Message };
            }
        }

        private async Task<List<SubFlowResult>> WaitByModeAsync(List<Task<SubFlowResult>> tasks, string waitMode, CancellationToken cancellationToken)
        {
            var results = new List<SubFlowResult>();

            switch (waitMode.ToUpperInvariant())
            {
                case "ALL":
                    // Wait for all tasks to complete
                    try
                    {
                        var allResults = await Task.WhenAll(tasks);
                        results.AddRange(allResults);
                    }
                    catch (OperationCanceledException)
                    {
                        // Collect whatever completed
                        foreach (var task in tasks)
                        {
                            if (task.IsCompletedSuccessfully)
                                results.Add(task.Result);
                            else
                                results.Add(new SubFlowResult { Success = false, Error = "Cancelled/Timeout" });
                        }
                    }
                    break;

                case "ANY":
                    // Wait for first successful completion
                    var remaining = new List<Task<SubFlowResult>>(tasks);
                    while (remaining.Count > 0)
                    {
                        var completed = await Task.WhenAny(remaining);
                        remaining.Remove(completed);
                        var result = await completed;
                        results.Add(result);
                        if (result.Success)
                            break; // First success, stop waiting
                    }
                    break;

                case "NONE":
                    // Don't wait, just return immediately (tasks continue in background)
                    // Results will be empty, but tasks are tracked
                    Console.WriteLine("[Parallel] WaitMode=None: sub-flows running in background");
                    break;

                default:
                    goto case "ALL";
            }

            return results;
        }

        private class SubFlowResult
        {
            public int Index { get; set; }
            public bool Success { get; set; }
            public string? Error { get; set; }
        }
    }
}
