namespace HWKUltra.Creator.Models
{
    /// <summary>
    /// Editor configuration loaded from JSON
    /// </summary>
    public class EditorConfig
    {
        public List<string> RecentFiles { get; set; } = new();
        public double CanvasGridSize { get; set; } = 20;
        public bool CanvasSnapToGrid { get; set; } = true;
        public string DefaultFlowDirectory { get; set; } = "ConfigJson/Flow";
        public string Theme { get; set; } = "Dark";
        public Dictionary<string, string> NodeCategoryColors { get; set; } = new()
        {
            ["Motion"] = "#FF5722",
            ["Camera"] = "#4CAF50",
            ["Measurement"] = "#673AB7",
            ["IO"] = "#607D8B",
            ["LightSource"] = "#FFC107",
            ["AutoFocus"] = "#00BCD4",
            ["Tray"] = "#795548",
            ["BarcodeScanner"] = "#3F51B5",
            ["Logic"] = "#9E9E9E",
            ["Synchronization"] = "#FF9800",
            ["Advanced"] = "#E91E63"
        };
    }
}
