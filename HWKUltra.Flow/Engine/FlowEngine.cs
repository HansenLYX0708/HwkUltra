using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Models;

namespace HWKUltra.Flow.Engine
{
    /// <summary>
    /// Flow engine - executes flow definitions
    /// </summary>
    public class FlowEngine
    {
        private readonly Dictionary<string, IFlowNode> _nodes = new();
        private readonly FlowDefinition _definition;

        /// <summary>
        /// Flow execution events
        /// </summary>
        public event EventHandler<FlowNodeEventArgs>? NodeExecuting;
        public event EventHandler<FlowNodeEventArgs>? NodeExecuted;
        public event EventHandler<FlowErrorEventArgs>? FlowError;
        public event EventHandler? FlowCompleted;

        public FlowEngine(FlowDefinition definition)
        {
            _definition = definition;
        }

        /// <summary>
        /// Register flow node
        /// </summary>
        public void RegisterNode(IFlowNode node)
        {
            _nodes[node.Id] = node;
        }

        /// <summary>
        /// Register multiple nodes
        /// </summary>
        public void RegisterNodes(IEnumerable<IFlowNode> nodes)
        {
            foreach (var node in nodes)
                _nodes[node.Id] = node;
        }

        /// <summary>
        /// Execute entire flow
        /// </summary>
        public async Task<FlowResult> ExecuteAsync(FlowContext? context = null, CancellationToken cancellationToken = default)
        {
            context ??= new FlowContext();
            context.CancellationToken = cancellationToken;

            var currentNodeId = _definition.StartNodeId;
            FlowResult? lastResult = null;

            try
            {
                while (currentNodeId != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_nodes.TryGetValue(currentNodeId, out var node))
                    {
                        throw new Exception($"Node not found: {currentNodeId}");
                    }

                    // Trigger pre-execution event
                    NodeExecuting?.Invoke(this, new FlowNodeEventArgs(node, context));

                    // Execute node
                    lastResult = await node.ExecuteAsync(context);

                    // Trigger post-execution event
                    NodeExecuted?.Invoke(this, new FlowNodeEventArgs(node, context, lastResult));

                    if (!lastResult.Success)
                    {
                        FlowError?.Invoke(this, new FlowErrorEventArgs(node, lastResult.ErrorMessage, context));
                        return lastResult;
                    }

                    // Determine next node
                    currentNodeId = GetNextNodeId(currentNodeId, lastResult);
                }

                FlowCompleted?.Invoke(this, EventArgs.Empty);
                return FlowResult.Ok();
            }
            catch (OperationCanceledException)
            {
                return FlowResult.Fail("Flow was cancelled");
            }
            catch (Exception ex)
            {
                FlowError?.Invoke(this, new FlowErrorEventArgs(null, ex.Message, context));
                return FlowResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get next node ID
        /// </summary>
        private string? GetNextNodeId(string currentNodeId, FlowResult result)
        {
            // If node specifies next node, use it first
            if (!string.IsNullOrEmpty(result.NextNodeId))
                return result.NextNodeId;

            // Find matching connection from connections
            var connection = _definition.Connections.FirstOrDefault(c =>
                c.SourceNodeId == currentNodeId &&
                ConditionMatches(result, c.Condition));

            return connection?.TargetNodeId;
        }

        /// <summary>
        /// Check if condition matches
        /// </summary>
        private bool ConditionMatches(FlowResult result, string? condition)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            // Match against BranchLabel first (for Loop/Branch nodes)
            if (!string.IsNullOrEmpty(result.BranchLabel))
                return string.Equals(result.BranchLabel, condition, StringComparison.OrdinalIgnoreCase);

            // Built-in conditions based on Success
            return condition switch
            {
                "Success" or "OK" => result.Success,
                "Fail" or "NG" => !result.Success,
                _ => false // Unmatched condition
            };
        }
    }

    /// <summary>
    /// Flow node event arguments
    /// </summary>
    public class FlowNodeEventArgs : EventArgs
    {
        public IFlowNode Node { get; }
        public FlowContext Context { get; }
        public FlowResult? Result { get; }

        public FlowNodeEventArgs(IFlowNode node, FlowContext context, FlowResult? result = null)
        {
            Node = node;
            Context = context;
            Result = result;
        }
    }

    /// <summary>
    /// Flow error event arguments
    /// </summary>
    public class FlowErrorEventArgs : EventArgs
    {
        public IFlowNode? Node { get; }
        public string ErrorMessage { get; }
        public FlowContext Context { get; }

        public FlowErrorEventArgs(IFlowNode? node, string errorMessage, FlowContext context)
        {
            Node = node;
            ErrorMessage = errorMessage;
            Context = context;
        }
    }
}
