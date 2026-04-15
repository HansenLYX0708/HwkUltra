namespace HWKUltra.UI.Models
{
    /// <summary>
    /// Application-wide persisted settings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Theme: "Light" or "Dark"
        /// </summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>
        /// Node catalog config file path (relative to exe dir or absolute)
        /// </summary>
        public string NodeCatalogConfigPath { get; set; } = "Local/configs/NodeCatalog.json";

        /// <summary>
        /// Flow definition default directory
        /// </summary>
        public string DefaultFlowDirectory { get; set; } = "ConfigJson/Flow";

        /// <summary>
        /// Canvas grid size
        /// </summary>
        public double CanvasGridSize { get; set; } = 20;

        /// <summary>
        /// Canvas snap to grid
        /// </summary>
        public bool CanvasSnapToGrid { get; set; } = true;

        /// <summary>
        /// Recent files list
        /// </summary>
        public List<string> RecentFiles { get; set; } = new();
    }
}
