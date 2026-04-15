using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Sub-flow node - loads and executes a child FlowDefinition sequentially.
    /// Enables flow reuse: define a test sequence once, call it from multiple places.
    /// The child flow shares the same SharedFlowContext and NodeFactory.
    /// </summary>
    public class SubFlowNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Sub-Flow";
        public override string NodeType => "SubFlow";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "FlowPath", DisplayName = "Flow Definition Path", Type = "string", Required = true, Description = "Path to the child flow JSON file" },
            new FlowParameter { Name = "PassVariables", DisplayName = "Pass Variables", Type = "string", Required = false, Description = "Comma-separated variable names to pass to child flow" },
            new FlowParameter { Name = "ReturnVariables", DisplayName = "Return Variables", Type = "string", Required = false, Description = "Comma-separated variable names to return from child flow" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool", Description = "Whether the sub-flow completed successfully" },
            new FlowParameter { Name = "ErrorMessage", DisplayName = "Error Message", Type = "string", Description = "Error message if sub-flow failed" },
            new FlowParameter { Name = "Duration", DisplayName = "Duration (ms)", Type = "int", Description = "Sub-flow execution duration" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var flowPath = context.GetNodeInput<string>(Id, "FlowPath") ?? "";
                var passVars = context.GetNodeInput<string>(Id, "PassVariables") ?? "";
                var returnVars = context.GetNodeInput<string>(Id, "ReturnVariables") ?? "";

                if (string.IsNullOrEmpty(flowPath))
                    return FlowResult.Fail("FlowPath is required");

                if (context.NodeFactory == null)
                    return FlowResult.Fail("NodeFactory is not set in FlowContext. Required for SubFlowNode.");

                // Load child flow definition
                if (!File.Exists(flowPath))
                    return FlowResult.Fail($"Flow definition file not found: {flowPath}");

                var definition = FlowSerializer.LoadFromFile(flowPath);
                if (definition == null)
                    return FlowResult.Fail($"Failed to deserialize flow definition: {flowPath}");

                // Create child flow engine
                var engine = new FlowEngine(definition);

                // Register nodes from definition
                foreach (var nodeDef in definition.Nodes)
                {
                    var node = context.NodeFactory.CreateNode(nodeDef.Type, nodeDef.Properties);
                    node.Id = nodeDef.Id;
                    node.Name = nodeDef.Name;
                    node.Description = nodeDef.Description;
                    engine.RegisterNode(node);
                }

                // Create child context
                var childContext = new FlowContext
                {
                    SharedContext = context.SharedContext,
                    NodeFactory = context.NodeFactory
                };

                // Inject node properties from definition
                foreach (var nodeDef in definition.Nodes)
                {
                    foreach (var prop in nodeDef.Properties)
                    {
                        childContext.Variables[$"{nodeDef.Id}:{prop.Key}"] = prop.Value;
                    }
                }

                // Pass specified variables from parent to child
                if (!string.IsNullOrWhiteSpace(passVars))
                {
                    foreach (var varName in passVars.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (context.Variables.TryGetValue(varName, out var val))
                            childContext.Variables[varName] = val;
                        // Also check shared context
                        else if (context.SharedContext != null && context.SharedContext.TryGetVariable<object>(varName, out var sharedVal) && sharedVal != null)
                            childContext.Variables[varName] = sharedVal;
                    }
                }

                // Execute child flow
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"[SubFlow] Starting: {definition.Name} ({flowPath})");

                var result = await engine.ExecuteAsync(childContext, context.CancellationToken);

                var duration = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                Console.WriteLine($"[SubFlow] Completed: {definition.Name} in {duration}ms, Success={result.Success}");

                // Return specified variables from child to parent
                if (!string.IsNullOrWhiteSpace(returnVars))
                {
                    foreach (var varName in returnVars.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (childContext.Variables.TryGetValue(varName, out var val))
                            context.Variables[varName] = val;
                    }
                }

                // Set outputs
                context.SetNodeOutput(Id, "Success", result.Success);
                context.SetNodeOutput(Id, "ErrorMessage", result.ErrorMessage ?? "");
                context.SetNodeOutput(Id, "Duration", duration);

                return result.Success ? FlowResult.Ok() : FlowResult.Fail(result.ErrorMessage ?? "Sub-flow failed");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"SubFlow execution failed: {ex.Message}");
            }
        }
    }
}
