using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Models
{
    /// <summary>
    /// Flow definition (for serialization and visual editing)
    /// </summary>
    public class FlowDefinition
    {
        /// <summary>
        /// Flow ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Flow name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Flow description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last modified time
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Version number
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Node list
        /// </summary>
        public List<NodeDefinition> Nodes { get; set; } = new();

        /// <summary>
        /// Connection list (relationships between nodes)
        /// </summary>
        public List<ConnectionDefinition> Connections { get; set; } = new();

        /// <summary>
        /// Start node ID
        /// </summary>
        public string? StartNodeId { get; set; }

        /// <summary>
        /// Global variable definitions
        /// </summary>
        public List<FlowParameter> GlobalVariables { get; set; } = new();

        /// <summary>
        /// Absolute path of the JSON file this definition was loaded from.
        /// Set automatically by <see cref="Utils.FlowSerializer.LoadFromFile"/>.
        /// Used to resolve relative sub-flow paths at runtime.
        /// Not serialized — runtime-only metadata.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string? SourceFilePath { get; set; }
    }

    /// <summary>
    /// Node definition (for serialization)
    /// </summary>
    public class NodeDefinition
    {
        /// <summary>
        /// Node ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Node type (e.g., Motion, Camera, Laser, Delay, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Node description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Position coordinates (for visual editing)
        /// </summary>
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// Whether the node ports are flipped (input on right, output on left)
        /// </summary>
        public bool IsFlipped { get; set; }

        /// <summary>
        /// Node property configuration
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Connection definition (link between nodes)
    /// </summary>
    public class ConnectionDefinition
    {
        /// <summary>
        /// Connection ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Source node ID
        /// </summary>
        public string SourceNodeId { get; set; } = string.Empty;

        /// <summary>
        /// Target node ID
        /// </summary>
        public string TargetNodeId { get; set; } = string.Empty;

        /// <summary>
        /// Condition expression (e.g., "Success", "Fail", "Result>0.5", etc.)
        /// </summary>
        public string? Condition { get; set; }
    }
}
