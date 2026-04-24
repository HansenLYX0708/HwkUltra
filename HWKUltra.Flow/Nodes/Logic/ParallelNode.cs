using System.Collections;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Parallel node — two complementary modes:
    ///
    /// <para><b>1. Static multi-flow (legacy)</b>: <c>FlowPaths</c> lists comma-separated
    /// JSON files; each runs once concurrently.</para>
    ///
    /// <para><b>2. Work-pool over an item source (new)</b>: when <c>ItemsSource</c> names
    /// a SharedContext variable containing a List or an <see cref="ImagePool"/>, a single
    /// worker flow (first entry of <c>FlowPaths</c>) is replicated across
    /// <c>WorkerCount</c> threads. Each thread pulls the next item from the source and
    /// runs the worker flow with <c>CurrentItem</c>, <c>CurrentIndex</c> and
    /// <c>TotalCount</c> injected into the child FlowContext. Streaming sources (pools)
    /// naturally terminate when <c>CompleteAdding</c> is called and the queue drains.</para>
    /// </summary>
    public class ParallelNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Parallel";
        public override string NodeType => "Parallel";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "FlowPaths",       DisplayName = "Flow Paths",       Type = "string", Required = true,  Description = "Comma-separated paths to worker flow JSON. In item-source mode only the first entry is used." },
            new FlowParameter { Name = "WaitMode",        DisplayName = "Wait Mode",        Type = "string", Required = false, DefaultValue = "All", Description = "All | Any | None (only applies in static multi-flow mode)" },
            new FlowParameter { Name = "TimeoutMs",       DisplayName = "Timeout (ms)",     Type = "int",    Required = false, DefaultValue = 0, Description = "Overall timeout in ms (0 = no timeout)" },
            new FlowParameter { Name = "ItemsSource",     DisplayName = "Items Source",     Type = "string", Required = false, Description = "SharedContext variable name holding a List<T> or an ImagePool" },
            new FlowParameter { Name = "WorkerCount",     DisplayName = "Worker Count",     Type = "int",    Required = false, DefaultValue = 0, Description = "Number of parallel workers consuming ItemsSource (0 = auto)" },
            new FlowParameter { Name = "ItemVariable",    DisplayName = "Item Variable",    Type = "string", Required = false, DefaultValue = "CurrentItem", Description = "Child-context variable name for the current item" },
            new FlowParameter { Name = "IndexVariable",   DisplayName = "Index Variable",   Type = "string", Required = false, DefaultValue = "CurrentIndex", Description = "Child-context variable name for the item index" },
            new FlowParameter { Name = "TotalVariable",   DisplayName = "Total Variable",   Type = "string", Required = false, DefaultValue = "TotalCount", Description = "Child-context variable name for the item total count (-1 if streaming unknown)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "CompletedCount", DisplayName = "Completed Count", Type = "int",  Description = "Successful sub-flow runs" },
            new FlowParameter { Name = "TotalCount",     DisplayName = "Total Count",     Type = "int",  Description = "Total sub-flow runs launched" },
            new FlowParameter { Name = "AllSuccess",     DisplayName = "All Success",     Type = "bool" },
            new FlowParameter { Name = "Errors",         DisplayName = "Errors",          Type = "string" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var flowPathsRaw = context.GetNodeInput<string>(Id, "FlowPaths") ?? "";
                var waitMode     = context.GetNodeInput<string>(Id, "WaitMode") ?? "All";
                var timeoutMs    = context.GetNodeInput<int>(Id, "TimeoutMs");
                var itemsSource  = context.GetNodeInput<string>(Id, "ItemsSource");

                if (string.IsNullOrEmpty(flowPathsRaw))
                    return FlowResult.Fail("FlowPaths is required");

                if (context.NodeFactory == null)
                    return FlowResult.Fail("NodeFactory is not set in FlowContext. Required for ParallelNode.");

                context.SharedContext ??= new SharedFlowContext();

                var flowPaths = flowPathsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (flowPaths.Length == 0)
                    return FlowResult.Fail("No flow paths specified");

                // Dispatch based on mode.
                if (!string.IsNullOrEmpty(itemsSource))
                    return await ExecuteItemSourceModeAsync(context, flowPaths[0], itemsSource, timeoutMs);
                else
                    return await ExecuteStaticModeAsync(context, flowPaths, waitMode, timeoutMs);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Parallel execution failed: {ex.Message}");
            }
        }

        // -------- Static (legacy) multi-flow mode --------
        private async Task<FlowResult> ExecuteStaticModeAsync(FlowContext context, string[] flowPaths, string waitMode, int timeoutMs)
        {
            Console.WriteLine($"[Parallel] Static mode: {flowPaths.Length} sub-flows, WaitMode={waitMode}");

            using var timeoutCts = timeoutMs > 0 ? new CancellationTokenSource(timeoutMs) : new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, timeoutCts.Token);

            var tasks = new List<Task<SubFlowResult>>();
            for (int i = 0; i < flowPaths.Length; i++)
            {
                var path = ResolveFlowPath(flowPaths[i], context.CurrentFlowDirectory);
                var index = i;
                tasks.Add(Task.Run(() => ExecuteSubFlowAsync(path, index, context, linkedCts.Token, item: null, itemIndex: -1, totalCount: -1, varNames: default), linkedCts.Token));
            }

            var results = await WaitByModeAsync(tasks, waitMode, linkedCts.Token);
            int completedCount = results.Count(r => r.Success);
            int totalCount = flowPaths.Length;
            bool allSuccess = completedCount == totalCount;
            var errors = string.Join(", ", results.Where(r => !r.Success).Select(r => r.Error));

            context.SetNodeOutput(Id, "CompletedCount", completedCount);
            context.SetNodeOutput(Id, "TotalCount", totalCount);
            context.SetNodeOutput(Id, "AllSuccess", allSuccess);
            context.SetNodeOutput(Id, "Errors", errors);

            Console.WriteLine($"[Parallel] Done: {completedCount}/{totalCount} succeeded");
            if (waitMode.Equals("All", StringComparison.OrdinalIgnoreCase) && !allSuccess)
                return FlowResult.Fail($"Parallel: {totalCount - completedCount} sub-flow(s) failed: {errors}");
            return FlowResult.Ok();
        }

        // -------- Work-pool item source mode --------
        private async Task<FlowResult> ExecuteItemSourceModeAsync(FlowContext context, string workerFlowPath, string itemsSource, int timeoutMs)
        {
            var workerCount    = context.GetNodeInput<int>(Id, "WorkerCount");
            var itemVar        = context.GetNodeInput<string>(Id, "ItemVariable");
            var indexVar       = context.GetNodeInput<string>(Id, "IndexVariable");
            var totalVar       = context.GetNodeInput<string>(Id, "TotalVariable");
            if (string.IsNullOrWhiteSpace(itemVar))  itemVar = "CurrentItem";
            if (string.IsNullOrWhiteSpace(indexVar)) indexVar = "CurrentIndex";
            if (string.IsNullOrWhiteSpace(totalVar)) totalVar = "TotalCount";

            // Poll for the ItemsSource to appear in SharedContext.
            // In producer-consumer pipelines the producer (which creates the pool)
            // and this consumer start in parallel — the pool may not exist yet.
            object? sourceObj = null;
            for (int retry = 0; retry < 100; retry++) // up to 10 seconds
            {
                sourceObj = context.SharedContext!.GetVariable<object>(itemsSource);
                if (sourceObj != null) break;
                await Task.Delay(100, context.CancellationToken);
            }
            if (sourceObj == null)
                return FlowResult.Fail($"ItemsSource '{itemsSource}' not found in SharedContext after waiting 10s");

            IItemSource src = sourceObj switch
            {
                ImagePool pool => new PoolItemSource(pool),
                IList list     => new ListItemSource(list),
                IEnumerable en => new ListItemSource(en.Cast<object>().ToList()),
                _ => throw new InvalidOperationException($"Unsupported ItemsSource type: {sourceObj.GetType().Name}. Expected List<T> or ImagePool.")
            };

            if (workerCount <= 0)
            {
                workerCount = src.TotalCount > 0
                    ? Math.Min(src.TotalCount, Environment.ProcessorCount)
                    : Math.Max(2, Environment.ProcessorCount);
            }

            using var timeoutCts = timeoutMs > 0 ? new CancellationTokenSource(timeoutMs) : new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, timeoutCts.Token);

            var resolvedPath = ResolveFlowPath(workerFlowPath, context.CurrentFlowDirectory);
            if (!File.Exists(resolvedPath))
                return FlowResult.Fail($"Worker flow not found: {workerFlowPath} (CurrentFlowDirectory='{context.CurrentFlowDirectory}')");

            Console.WriteLine($"[Parallel] ItemSource mode: source={itemsSource} ({sourceObj.GetType().Name}), workers={workerCount}, totalHint={src.TotalCount}");

            int processed = 0, failed = 0;
            var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
            var varNames = (itemVar!, indexVar!, totalVar!);

            var workers = new List<Task>();
            for (int w = 0; w < workerCount; w++)
            {
                int workerId = w;
                workers.Add(Task.Run(async () =>
                {
                    while (!linkedCts.IsCancellationRequested)
                    {
                        if (!src.TryTakeNext(linkedCts.Token, out var item, out var idx))
                            break; // source exhausted / completed

                        var result = await ExecuteSubFlowAsync(resolvedPath, workerId, context, linkedCts.Token, item, idx, src.TotalCount, varNames);
                        if (result.Success) Interlocked.Increment(ref processed);
                        else
                        {
                            Interlocked.Increment(ref failed);
                            if (!string.IsNullOrEmpty(result.Error)) errors.Add(result.Error!);
                        }
                    }
                }, linkedCts.Token));
            }

            await Task.WhenAll(workers);

            int total = processed + failed;
            bool allSuccess = failed == 0;
            context.SetNodeOutput(Id, "CompletedCount", processed);
            context.SetNodeOutput(Id, "TotalCount", total);
            context.SetNodeOutput(Id, "AllSuccess", allSuccess);
            context.SetNodeOutput(Id, "Errors", string.Join(", ", errors.Take(10)));

            Console.WriteLine($"[Parallel] ItemSource done: {processed}/{total} succeeded, {failed} failed");
            return allSuccess ? FlowResult.Ok() : FlowResult.Fail($"Parallel: {failed} worker runs failed");
        }

        // -------- Sub-flow execution (common) --------
        private async Task<SubFlowResult> ExecuteSubFlowAsync(
            string flowPath, int index, FlowContext parentContext, CancellationToken cancellationToken,
            object? item, int itemIndex, int totalCount, (string item, string index, string total) varNames)
        {
            try
            {
                if (!File.Exists(flowPath))
                    return new SubFlowResult { Index = index, Success = false, Error = $"File not found: {flowPath}" };

                var definition = FlowSerializer.LoadFromFile(flowPath);
                if (definition == null)
                    return new SubFlowResult { Index = index, Success = false, Error = $"Failed to load: {flowPath}" };

                var engine = new FlowEngine(definition);
                foreach (var nodeDef in definition.Nodes)
                {
                    var node = parentContext.NodeFactory!.CreateNode(nodeDef.Type, nodeDef.Properties);
                    node.Id = nodeDef.Id;
                    node.Name = nodeDef.Name;
                    node.Description = nodeDef.Description;
                    engine.RegisterNode(node);
                }

                var childContext = new FlowContext
                {
                    SharedContext = parentContext.SharedContext,
                    NodeFactory = parentContext.NodeFactory,
                    CurrentFlowDirectory = Path.GetDirectoryName(Path.GetFullPath(flowPath)),
                    OnNodeLog = parentContext.OnNodeLog
                };

                // Wire child engine events to OnNodeLog callback
                var flowName = definition.Name ?? Path.GetFileNameWithoutExtension(flowPath);
                if (parentContext.OnNodeLog != null)
                {
                    engine.NodeExecuting += (_, e) =>
                        parentContext.OnNodeLog(flowName, e.Node.Name, e.Node.NodeType, true, null);
                    engine.NodeExecuted += (_, e) =>
                        parentContext.OnNodeLog(flowName, e.Node.Name, e.Node.NodeType, false, e.Result);
                }

                // Inject node properties
                foreach (var nodeDef in definition.Nodes)
                    foreach (var prop in nodeDef.Properties)
                        childContext.Variables[$"{nodeDef.Id}:{prop.Key}"] = prop.Value;

                // Inject per-item variables (item-source mode only)
                if (item != null && !string.IsNullOrEmpty(varNames.item))
                {
                    // Unwrap PoolItem → Bitmap for downstream vision nodes that expect Bitmap/path/etc.
                    childContext.Variables[varNames.item] = item is PoolItem pi ? pi.Bitmap : item;
                    childContext.Variables[varNames.index] = itemIndex;
                    childContext.Variables[varNames.total] = totalCount;
                    // Also publish the raw PoolItem (for nodes that need access to metadata / Dispose).
                    if (item is PoolItem pi2) childContext.Variables[varNames.item + "_Item"] = pi2;
                }

                var result = await engine.ExecuteAsync(childContext, cancellationToken);
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
                    try
                    {
                        var all = await Task.WhenAll(tasks);
                        results.AddRange(all);
                    }
                    catch (OperationCanceledException)
                    {
                        foreach (var t in tasks)
                            results.Add(t.IsCompletedSuccessfully ? t.Result : new SubFlowResult { Success = false, Error = "Cancelled/Timeout" });
                    }
                    break;
                case "ANY":
                    var remaining = new List<Task<SubFlowResult>>(tasks);
                    while (remaining.Count > 0)
                    {
                        var done = await Task.WhenAny(remaining);
                        remaining.Remove(done);
                        var r = await done;
                        results.Add(r);
                        if (r.Success) break;
                    }
                    break;
                case "NONE":
                    Console.WriteLine("[Parallel] WaitMode=None: sub-flows running in background");
                    break;
                default: goto case "ALL";
            }
            return results;
        }

        /// <summary>
        /// Resolve a worker flow path. Shares logic with <see cref="SubFlowNode.ResolveFlowPath"/>:
        ///   1. As-is, 2. relative to the parent flow's directory, 3. AppContext.BaseDirectory / ConfigJson/Flow.
        /// </summary>
        private static string ResolveFlowPath(string path, string? currentFlowDir)
            => SubFlowNode.ResolveFlowPath(path, currentFlowDir);

        private class SubFlowResult
        {
            public int Index { get; set; }
            public bool Success { get; set; }
            public string? Error { get; set; }
        }

        // ---------- Item source adapters ----------
        private interface IItemSource
        {
            int TotalCount { get; }
            bool TryTakeNext(CancellationToken ct, out object? item, out int index);
        }

        private sealed class ListItemSource : IItemSource
        {
            private readonly IList _items;
            private int _next = -1;
            public ListItemSource(IList items) { _items = items; }
            public int TotalCount => _items.Count;
            public bool TryTakeNext(CancellationToken ct, out object? item, out int index)
            {
                int i = Interlocked.Increment(ref _next);
                if (i >= _items.Count) { item = null; index = -1; return false; }
                item = _items[i];
                index = i;
                return true;
            }
        }

        private sealed class PoolItemSource : IItemSource
        {
            private readonly ImagePool _pool;
            public PoolItemSource(ImagePool pool) { _pool = pool; }
            public int TotalCount => -1; // unknown for streaming
            public bool TryTakeNext(CancellationToken ct, out object? item, out int index)
            {
                try
                {
                    // BlockingCollection.Take via pool extension method
                    foreach (var pi in _pool.GetConsumingEnumerable(ct))
                    {
                        item = pi;
                        index = pi.Index;
                        return true;
                    }
                }
                catch (OperationCanceledException) { }
                item = null; index = -1;
                return false;
            }
        }
    }
}
