namespace HWKUltra.UI.Models
{
    /// <summary>
    /// Root config for node catalog - loaded from JSON
    /// </summary>
    public class NodeCatalogConfig
    {
        /// <summary>
        /// Category definitions with visual properties
        /// </summary>
        public List<NodeCategoryConfig> Categories { get; set; } = new();

        /// <summary>
        /// Node type entries referencing Flow project NodeType
        /// </summary>
        public List<NodeTypeConfig> Nodes { get; set; } = new();
    }

    /// <summary>
    /// Category configuration with visual properties
    /// </summary>
    public class NodeCategoryConfig
    {
        /// <summary>
        /// Category name (must match node Category references)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display color (hex, e.g., "#FF5722")
        /// </summary>
        public string Color { get; set; } = "#2196F3";

        /// <summary>
        /// Display order in toolbox (lower = higher priority)
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Node type configuration - maps to Flow project's IFlowNode NodeType
    /// </summary>
    public class NodeTypeConfig
    {
        /// <summary>
        /// Node type key (must match DefaultNodeFactory switch case, e.g., "AxisHome")
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Display name in toolbox
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Category this node belongs to (must match a NodeCategoryConfig.Name)
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Override color for this node (null to use category color)
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Default width in visual editor
        /// </summary>
        public double DefaultWidth { get; set; } = 160;

        /// <summary>
        /// Default height in visual editor
        /// </summary>
        public double DefaultHeight { get; set; } = 80;

        /// <summary>
        /// Whether this node type is visible in the toolbox
        /// </summary>
        public bool Visible { get; set; } = true;
    }
}
