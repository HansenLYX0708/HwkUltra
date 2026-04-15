using HWKUltra.Flow.Abstractions;

namespace HWKUltra.UI.Models
{
    /// <summary>
    /// Represents an available node type in the toolbox
    /// </summary>
    public class NodeCatalogEntry
    {
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Color { get; set; } = "#2196F3";
        public string? Description { get; set; }
        public double DefaultWidth { get; set; } = 160;
        public double DefaultHeight { get; set; } = 80;
        public List<FlowParameter> InputDefinitions { get; set; } = new();
        public List<FlowParameter> OutputDefinitions { get; set; } = new();
    }

    /// <summary>
    /// Category grouping for the toolbox
    /// </summary>
    public class NodeCatalogCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#2196F3";
        public List<NodeCatalogEntry> Entries { get; set; } = new();
    }
}
