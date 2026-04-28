using System.IO;
using System.Text.Json;
using HWKUltra.Flow.Services;
using HWKUltra.UI.Models;

namespace HWKUltra.UI.Services
{
    /// <summary>
    /// Loads node catalog from JSON config file and enriches with Flow node definitions
    /// </summary>
    public class NodeCatalogService
    {
        private readonly AppSettingsService _settingsService;
        private List<NodeCatalogCategory>? _cachedCategories;
        private NodeCatalogConfig? _catalogConfig;

        public NodeCatalogService(AppSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Get all node catalog categories with entries populated from JSON config + Flow node definitions
        /// </summary>
        public List<NodeCatalogCategory> GetCategories()
        {
            if (_cachedCategories != null)
                return _cachedCategories;

            var config = LoadCatalogConfig();
            var factory = new DefaultNodeFactory();

            // Build category lookup from config
            var categoryLookup = config.Categories
                .OrderBy(c => c.Order)
                .ToDictionary(c => c.Name, c => c);

            var categories = new Dictionary<string, NodeCatalogCategory>();

            foreach (var nodeCfg in config.Nodes.Where(n => n.Visible))
            {
                // Ensure category exists
                if (!categories.TryGetValue(nodeCfg.Category, out var cat))
                {
                    var catCfg = categoryLookup.GetValueOrDefault(nodeCfg.Category);
                    var catColor = catCfg?.Color ?? "#2196F3";
                    cat = new NodeCatalogCategory { Name = nodeCfg.Category, Color = catColor };
                    categories[nodeCfg.Category] = cat;
                }

                try
                {
                    // Create a temporary simulation node to get parameter definitions from Flow project
                    var node = factory.CreateNode(nodeCfg.Type, new Dictionary<string, string>(), useSimulation: true);

                    // Sync visual properties from config back to node (for consistency)
                    node.Category = nodeCfg.Category;
                    node.Color = nodeCfg.Color ?? cat.Color;
                    node.DefaultWidth = nodeCfg.DefaultWidth;
                    node.DefaultHeight = nodeCfg.DefaultHeight;

                    var entry = new NodeCatalogEntry
                    {
                        Type = nodeCfg.Type,
                        DisplayName = nodeCfg.DisplayName,
                        Category = nodeCfg.Category,
                        Color = nodeCfg.Color ?? cat.Color,
                        Description = node.Description,
                        DefaultWidth = nodeCfg.DefaultWidth,
                        DefaultHeight = nodeCfg.DefaultHeight,
                        InputDefinitions = node.Inputs?.ToList() ?? new(),
                        OutputDefinitions = node.Outputs?.ToList() ?? new()
                    };
                    cat.Entries.Add(entry);
                }
                catch
                {
                    // Node type not found in factory - add basic entry
                    cat.Entries.Add(new NodeCatalogEntry
                    {
                        Type = nodeCfg.Type,
                        DisplayName = nodeCfg.DisplayName,
                        Category = nodeCfg.Category,
                        Color = nodeCfg.Color ?? cat.Color,
                        DefaultWidth = nodeCfg.DefaultWidth,
                        DefaultHeight = nodeCfg.DefaultHeight
                    });
                }
            }

            // Sort categories by configured order
            var orderedCategories = categories.Values
                .OrderBy(c => categoryLookup.GetValueOrDefault(c.Name)?.Order ?? int.MaxValue)
                .ToList();

            _cachedCategories = orderedCategories;
            return _cachedCategories;
        }

        /// <summary>
        /// Find a catalog entry by node type
        /// </summary>
        public NodeCatalogEntry? FindEntry(string type)
        {
            return GetCategories()
                .SelectMany(c => c.Entries)
                .FirstOrDefault(e => e.Type == type);
        }

        /// <summary>
        /// Invalidate cache and reload from config (e.g., after config path change)
        /// </summary>
        public void InvalidateCache()
        {
            _cachedCategories = null;
            _catalogConfig = null;
        }

        private NodeCatalogConfig LoadCatalogConfig()
        {
            if (_catalogConfig != null)
                return _catalogConfig;

            var settings = _settingsService.Settings;
            var configPath = _settingsService.ResolvePath(settings.NodeCatalogConfigPath);

            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    _catalogConfig = JsonSerializer.Deserialize<NodeCatalogConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch
            {
                // Fall back to defaults
            }

            _catalogConfig ??= GetDefaultCatalogConfig();
            return _catalogConfig;
        }

        /// <summary>
        /// Default catalog config matching all Flow project node types
        /// </summary>
        private static NodeCatalogConfig GetDefaultCatalogConfig()
        {
            return new NodeCatalogConfig
            {
                Categories = new List<NodeCategoryConfig>
                {
                    new() { Name = "Motion", Color = "#FF5722", Order = 0 },
                    new() { Name = "Camera", Color = "#4CAF50", Order = 1 },
                    new() { Name = "Measurement", Color = "#673AB7", Order = 2 },
                    new() { Name = "IO", Color = "#607D8B", Order = 3 },
                    new() { Name = "LightSource", Color = "#FFC107", Order = 4 },
                    new() { Name = "AutoFocus", Color = "#00BCD4", Order = 5 },
                    new() { Name = "Tray", Color = "#795548", Order = 6 },
                    new() { Name = "BarcodeScanner", Color = "#3F51B5", Order = 7 },
                    new() { Name = "Logic", Color = "#9E9E9E", Order = 8 },
                    new() { Name = "Synchronization", Color = "#FF9800", Order = 9 },
                    new() { Name = "Advanced", Color = "#E91E63", Order = 10 }
                },
                Nodes = new List<NodeTypeConfig>
                {
                    // Motion
                    new() { Type = "AxisHome", DisplayName = "Axis Home", Category = "Motion" },
                    new() { Type = "AxisMoveAbs", DisplayName = "Axis Move Absolute", Category = "Motion" },
                    new() { Type = "AxisMoveRel", DisplayName = "Axis Move Relative", Category = "Motion" },
                    new() { Type = "AxisMoveVelocity", DisplayName = "Axis Move Velocity", Category = "Motion" },
                    new() { Type = "AxisWaitInPos", DisplayName = "Axis Wait In Position", Category = "Motion" },
                    new() { Type = "GroupInterpolation", DisplayName = "Group Interpolation", Category = "Motion" },

                    // Camera
                    new() { Type = "CameraOpen", DisplayName = "Camera Open", Category = "Camera" },
                    new() { Type = "CameraClose", DisplayName = "Camera Close", Category = "Camera" },
                    new() { Type = "CameraTrigger", DisplayName = "Camera Trigger", Category = "Camera" },
                    new() { Type = "CameraGrab", DisplayName = "Camera Grab", Category = "Camera" },
                    new() { Type = "CameraSetExposure", DisplayName = "Camera Set Exposure", Category = "Camera" },
                    new() { Type = "CameraSetGain", DisplayName = "Camera Set Gain", Category = "Camera" },
                    new() { Type = "CameraSetTriggerMode", DisplayName = "Camera Set Trigger Mode", Category = "Camera" },

                    // Measurement
                    new() { Type = "MeasurementOpen", DisplayName = "Measurement Open", Category = "Measurement" },
                    new() { Type = "MeasurementClose", DisplayName = "Measurement Close", Category = "Measurement" },
                    new() { Type = "MeasurementGetData", DisplayName = "Measurement Get Data", Category = "Measurement" },
                    new() { Type = "MeasurementStartStorage", DisplayName = "Measurement Start Storage", Category = "Measurement" },
                    new() { Type = "MeasurementStopStorage", DisplayName = "Measurement Stop Storage", Category = "Measurement" },
                    new() { Type = "MeasurementClearStorage", DisplayName = "Measurement Clear Storage", Category = "Measurement" },
                    new() { Type = "MeasurementGetTrendData", DisplayName = "Measurement Get Trend Data", Category = "Measurement" },
                    new() { Type = "MeasurementSetSampling", DisplayName = "Measurement Set Sampling", Category = "Measurement" },
                    new() { Type = "MeasurementControl", DisplayName = "Measurement Control", Category = "Measurement" },

                    // IO
                    new() { Type = "DigitalOutput", DisplayName = "Digital Output", Category = "IO" },
                    new() { Type = "DigitalInput", DisplayName = "Digital Input", Category = "IO" },

                    // LightSource
                    new() { Type = "LightSetTriggerMode", DisplayName = "Light Set Trigger Mode", Category = "LightSource" },
                    new() { Type = "LightSetContinuousMode", DisplayName = "Light Set Continuous Mode", Category = "LightSource" },
                    new() { Type = "LightTurnOnOff", DisplayName = "Light Turn On/Off", Category = "LightSource" },

                    // AutoFocus
                    new() { Type = "AutoFocusOpen", DisplayName = "AutoFocus Open", Category = "AutoFocus" },
                    new() { Type = "AutoFocusClose", DisplayName = "AutoFocus Close", Category = "AutoFocus" },
                    new() { Type = "AutoFocusEnable", DisplayName = "AutoFocus Enable", Category = "AutoFocus" },
                    new() { Type = "AutoFocusDisable", DisplayName = "AutoFocus Disable", Category = "AutoFocus" },
                    new() { Type = "AutoFocusLaserOn", DisplayName = "AutoFocus Laser On", Category = "AutoFocus" },
                    new() { Type = "AutoFocusLaserOff", DisplayName = "AutoFocus Laser Off", Category = "AutoFocus" },
                    new() { Type = "AutoFocusGetStatus", DisplayName = "AutoFocus Get Status", Category = "AutoFocus" },
                    new() { Type = "AutoFocusCommand", DisplayName = "AutoFocus Command", Category = "AutoFocus" },
                    new() { Type = "AutoFocusReset", DisplayName = "AutoFocus Reset", Category = "AutoFocus" },

                    // Tray
                    new() { Type = "TrayInit", DisplayName = "Tray Initialize", Category = "Tray" },
                    new() { Type = "TrayTeach", DisplayName = "Tray Teach", Category = "Tray" },
                    new() { Type = "TrayGetPosition", DisplayName = "Tray Get Position", Category = "Tray" },
                    new() { Type = "TraySetSlotState", DisplayName = "Tray Set Slot State", Category = "Tray" },
                    new() { Type = "TrayGetSlotState", DisplayName = "Tray Get Slot State", Category = "Tray" },
                    new() { Type = "TrayReset", DisplayName = "Tray Reset", Category = "Tray" },
                    new() { Type = "TrayGetInfo", DisplayName = "Tray Get Info", Category = "Tray" },
                    new() { Type = "TrayIterator", DisplayName = "Tray Iterator", Category = "Tray" },

                    // BarcodeScanner
                    new() { Type = "BarcodeScannerOpen", DisplayName = "BarcodeScanner Open", Category = "BarcodeScanner" },
                    new() { Type = "BarcodeScannerClose", DisplayName = "BarcodeScanner Close", Category = "BarcodeScanner" },
                    new() { Type = "BarcodeScannerTrigger", DisplayName = "BarcodeScanner Trigger", Category = "BarcodeScanner" },
                    new() { Type = "BarcodeScannerGetLast", DisplayName = "BarcodeScanner Get Last", Category = "BarcodeScanner" },

                    // Logic
                    new() { Type = "Delay", DisplayName = "Delay", Category = "Logic" },
                    new() { Type = "Branch", DisplayName = "Branch", Category = "Logic" },
                    new() { Type = "Loop", DisplayName = "Loop", Category = "Logic" },
                    new() { Type = "SubFlow", DisplayName = "Sub-Flow", Category = "Logic", DefaultWidth = 180, DefaultHeight = 90 },
                    new() { Type = "Parallel", DisplayName = "Parallel Execution", Category = "Logic", DefaultWidth = 200, DefaultHeight = 100 },
                    new() { Type = "IncrementSharedVariable", DisplayName = "Increment Shared Variable", Category = "Logic" },
                    new() { Type = "AppendToList", DisplayName = "Append To List", Category = "Logic" },
                    new() { Type = "ClearList", DisplayName = "Clear List", Category = "Logic" },
                    new() { Type = "ListLookupByIndex", DisplayName = "List Lookup By Index", Category = "Logic" },
                    new() { Type = "SaveResultsToCsv", DisplayName = "Save Results To CSV", Category = "Logic" },
                    new() { Type = "ImagePoolCreate", DisplayName = "Image Pool Create", Category = "Logic" },
                    new() { Type = "ImagePoolComplete", DisplayName = "Image Pool Complete", Category = "Logic" },
                    new() { Type = "ImagePoolClose", DisplayName = "Image Pool Close", Category = "Logic" },

                    // Synchronization
                    new() { Type = "SetSignal", DisplayName = "Set Signal", Category = "Synchronization" },
                    new() { Type = "WaitForSignal", DisplayName = "Wait For Signal", Category = "Synchronization" },
                    new() { Type = "AcquireLock", DisplayName = "Acquire Lock", Category = "Synchronization" },
                    new() { Type = "ReleaseLock", DisplayName = "Release Lock", Category = "Synchronization" },
                    new() { Type = "SetSharedVariable", DisplayName = "Set Shared Variable", Category = "Synchronization" },
                    new() { Type = "GetSharedVariable", DisplayName = "Get Shared Variable", Category = "Synchronization" },

                    // Advanced
                    new() { Type = "OnTheFlyCapture", DisplayName = "On-The-Fly Capture", Category = "Advanced" }
                }
            };
        }
    }
}
