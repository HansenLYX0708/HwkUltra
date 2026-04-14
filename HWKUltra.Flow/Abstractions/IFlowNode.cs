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

        /// <summary>
        /// Get node-scoped input variable (reads from "{nodeId}:{key}")
        /// </summary>
        public T? GetNodeInput<T>(string nodeId, string key)
        {
            var scopedKey = $"{nodeId}:{key}";
            if (Variables.TryGetValue(scopedKey, out var value))
            {
                if (value is T t) return t;
                // string → target type conversion for properties from NodeDefinition
                if (value is string s && typeof(T) != typeof(string))
                {
                    try
                    {
                        var converted = Convert.ChangeType(s, typeof(T));
                        if (converted is T ct) return ct;
                    }
                    catch { }
                }
            }
            return default;
        }

        /// <summary>
        /// Set node-scoped output variable (writes to "{nodeId}:{key}")
        /// </summary>
        public void SetNodeOutput<T>(string nodeId, string key, T value)
        {
            Variables[$"{nodeId}:{key}"] = value!;
        }

        /// <summary>
        /// Find a variable by short key across all scopes.
        /// Searches: exact key first, then any "{nodeId}:{key}" match.
        /// Useful for cross-node references (e.g. BranchNode.Condition).
        /// </summary>
        public T? FindVariable<T>(string key)
        {
            // 1. Exact global match
            if (Variables.TryGetValue(key, out var val))
            {
                if (val is T t) return t;
                if (val is string s && typeof(T) != typeof(string))
                {
                    try { var c = Convert.ChangeType(s, typeof(T)); if (c is T ct) return ct; } catch { }
                }
            }
            // 2. Search scoped keys (last writer wins)
            var suffix = $":{key}";
            foreach (var kv in Variables)
            {
                if (kv.Key.EndsWith(suffix))
                {
                    if (kv.Value is T t2) return t2;
                    if (kv.Value is string s2 && typeof(T) != typeof(string))
                    {
                        try { var c = Convert.ChangeType(s2, typeof(T)); if (c is T ct2) return ct2; } catch { }
                    }
                }
            }
            return default;
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
        /// Next node ID (null to execute default next node via connections)
        /// </summary>
        public string? NextNodeId { get; set; }

        /// <summary>
        /// Branch label for conditional routing (matched against Connection.Condition)
        /// </summary>
        public string? BranchLabel { get; set; }

        public static FlowResult Ok() => new() { Success = true };
        public static FlowResult OkBranch(string branchLabel) => new() { Success = true, BranchLabel = branchLabel };
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
