namespace HWKUltra.Flow.Abstractions
{
    /// <summary>
    /// Flow node execution context
    /// </summary>
    public class FlowContext
    {
        /// <summary>
        /// Flow instance ID
        /// </summary>
        public Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        /// Variable storage (for data transfer between nodes)
        /// </summary>
        public Dictionary<string, object> Variables { get; } = new();

        /// <summary>
        /// Execution result data
        /// </summary>
        public Dictionary<string, object> Results { get; } = new();

        /// <summary>
        /// Cancellation token
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Get variable value
        /// </summary>
        public T? GetVariable<T>(string key)
        {
            if (Variables.TryGetValue(key, out var value) && value is T t)
                return t;
            return default;
        }

        /// <summary>
        /// Set variable value
        /// </summary>
        public void SetVariable<T>(string key, T value)
        {
            Variables[key] = value!;
        }
    }

    /// <summary>
    /// Flow node execution result
    /// </summary>
    public class FlowResult
    {
        /// <summary>
        /// Whether the execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Output data
        /// </summary>
        public Dictionary<string, object> Outputs { get; } = new();

        /// <summary>
        /// Next node ID (null to execute default next node)
        /// </summary>
        public string? NextNodeId { get; set; }

        public static FlowResult Ok() => new() { Success = true };
        public static FlowResult Ok(string nextNodeId) => new() { Success = true, NextNodeId = nextNodeId };
        public static FlowResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Flow node interface
    /// </summary>
    public interface IFlowNode
    {
        /// <summary>
        /// Node ID (unique identifier)
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Node name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Node type
        /// </summary>
        string NodeType { get; }

        /// <summary>
        /// Node description
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// Input parameter definitions (for visual editing)
        /// </summary>
        List<FlowParameter> Inputs { get; }

        /// <summary>
        /// Output parameter definitions
        /// </summary>
        List<FlowParameter> Outputs { get; }

        /// <summary>
        /// Execute node
        /// </summary>
        Task<FlowResult> ExecuteAsync(FlowContext context);
    }

    /// <summary>
    /// Flow parameter definition
    /// </summary>
    public class FlowParameter
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = "string"; // string, int, double, bool, position, etc.
        public bool Required { get; set; } = false;
        public object? DefaultValue { get; set; }
        public string? Description { get; set; }
    }
}
