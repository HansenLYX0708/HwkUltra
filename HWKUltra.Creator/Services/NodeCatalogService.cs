using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Services;
using HWKUltra.Creator.Models;

namespace HWKUltra.Creator.Services
{
    /// <summary>
    /// Discovers available node types from DefaultNodeFactory and provides catalog entries
    /// </summary>
    public class NodeCatalogService
    {
        private readonly EditorConfigService _configService;
        private List<NodeCatalogCategory>? _cachedCategories;

        /// <summary>
        /// All registered node type definitions: Type → (Category, DisplayName)
        /// Add new node types here when extending the system.
        /// </summary>
        private static readonly List<(string Type, string Category, string DisplayName)> _nodeTypeRegistry = new()
        {
            // Motion
            ("AxisHome", "Motion", "Axis Home"),
            ("AxisMoveAbs", "Motion", "Axis Move Absolute"),
            ("AxisMoveRel", "Motion", "Axis Move Relative"),
            ("AxisMoveVelocity", "Motion", "Axis Move Velocity"),
            ("AxisWaitInPos", "Motion", "Axis Wait In Position"),
            ("GroupInterpolation", "Motion", "Group Interpolation"),

            // Camera
            ("CameraOpen", "Camera", "Camera Open"),
            ("CameraClose", "Camera", "Camera Close"),
            ("CameraTrigger", "Camera", "Camera Trigger"),
            ("CameraGrab", "Camera", "Camera Grab"),
            ("CameraSetExposure", "Camera", "Camera Set Exposure"),
            ("CameraSetGain", "Camera", "Camera Set Gain"),
            ("CameraSetTriggerMode", "Camera", "Camera Set Trigger Mode"),

            // Measurement
            ("MeasurementOpen", "Measurement", "Measurement Open"),
            ("MeasurementClose", "Measurement", "Measurement Close"),
            ("MeasurementGetData", "Measurement", "Measurement Get Data"),
            ("MeasurementStartStorage", "Measurement", "Measurement Start Storage"),
            ("MeasurementStopStorage", "Measurement", "Measurement Stop Storage"),
            ("MeasurementClearStorage", "Measurement", "Measurement Clear Storage"),
            ("MeasurementGetTrendData", "Measurement", "Measurement Get Trend Data"),
            ("MeasurementSetSampling", "Measurement", "Measurement Set Sampling"),
            ("MeasurementControl", "Measurement", "Measurement Control"),

            // IO
            ("DigitalOutput", "IO", "Digital Output"),
            ("DigitalInput", "IO", "Digital Input"),

            // LightSource
            ("LightSetTriggerMode", "LightSource", "Light Set Trigger Mode"),
            ("LightSetContinuousMode", "LightSource", "Light Set Continuous Mode"),
            ("LightTurnOnOff", "LightSource", "Light Turn On/Off"),

            // AutoFocus
            ("AutoFocusOpen", "AutoFocus", "AutoFocus Open"),
            ("AutoFocusClose", "AutoFocus", "AutoFocus Close"),
            ("AutoFocusEnable", "AutoFocus", "AutoFocus Enable"),
            ("AutoFocusDisable", "AutoFocus", "AutoFocus Disable"),
            ("AutoFocusLaserOn", "AutoFocus", "AutoFocus Laser On"),
            ("AutoFocusLaserOff", "AutoFocus", "AutoFocus Laser Off"),
            ("AutoFocusGetStatus", "AutoFocus", "AutoFocus Get Status"),
            ("AutoFocusCommand", "AutoFocus", "AutoFocus Command"),
            ("AutoFocusReset", "AutoFocus", "AutoFocus Reset"),

            // Tray
            ("TrayInit", "Tray", "Tray Initialize"),
            ("TrayTeach", "Tray", "Tray Teach"),
            ("TrayGetPosition", "Tray", "Tray Get Position"),
            ("TraySetSlotState", "Tray", "Tray Set Slot State"),
            ("TrayGetSlotState", "Tray", "Tray Get Slot State"),
            ("TrayReset", "Tray", "Tray Reset"),
            ("TrayGetInfo", "Tray", "Tray Get Info"),
            ("TrayIterator", "Tray", "Tray Iterator"),

            // BarcodeScanner
            ("BarcodeScannerOpen", "BarcodeScanner", "BarcodeScanner Open"),
            ("BarcodeScannerClose", "BarcodeScanner", "BarcodeScanner Close"),
            ("BarcodeScannerTrigger", "BarcodeScanner", "BarcodeScanner Trigger"),
            ("BarcodeScannerGetLast", "BarcodeScanner", "BarcodeScanner Get Last"),

            // Logic
            ("Delay", "Logic", "Delay"),
            ("Branch", "Logic", "Branch"),
            ("Loop", "Logic", "Loop"),
            ("SubFlow", "Logic", "Sub-Flow"),
            ("Parallel", "Logic", "Parallel Execution"),

            // Synchronization
            ("SetSignal", "Synchronization", "Set Signal"),
            ("WaitForSignal", "Synchronization", "Wait For Signal"),
            ("AcquireLock", "Synchronization", "Acquire Lock"),
            ("ReleaseLock", "Synchronization", "Release Lock"),
            ("SetSharedVariable", "Synchronization", "Set Shared Variable"),
            ("GetSharedVariable", "Synchronization", "Get Shared Variable"),

            // Advanced
            ("OnTheFlyCapture", "Advanced", "On-The-Fly Capture"),
        };

        public NodeCatalogService(EditorConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Get all node catalog categories with entries populated from the node factory
        /// </summary>
        public List<NodeCatalogCategory> GetCategories()
        {
            if (_cachedCategories != null)
                return _cachedCategories;

            var config = _configService.GetConfig();
            // Create a simulation-mode factory (all null services) to introspect node definitions
            var factory = new DefaultNodeFactory();

            var categories = new Dictionary<string, NodeCatalogCategory>();

            foreach (var (type, category, displayName) in _nodeTypeRegistry)
            {
                if (!categories.TryGetValue(category, out var cat))
                {
                    var color = config.NodeCategoryColors.TryGetValue(category, out var c) ? c : "#2196F3";
                    cat = new NodeCatalogCategory { Name = category, Color = color };
                    categories[category] = cat;
                }

                try
                {
                    // Create a temporary node instance to get its parameter definitions
                    var node = factory.CreateNode(type, new Dictionary<string, string>(), useSimulation: true);
                    var entry = new NodeCatalogEntry
                    {
                        Type = type,
                        DisplayName = displayName,
                        Category = category,
                        Color = cat.Color,
                        Description = node.Description,
                        InputDefinitions = node.Inputs?.ToList() ?? new(),
                        OutputDefinitions = node.Outputs?.ToList() ?? new()
                    };
                    cat.Entries.Add(entry);
                }
                catch
                {
                    // If node creation fails, still add a basic entry
                    cat.Entries.Add(new NodeCatalogEntry
                    {
                        Type = type,
                        DisplayName = displayName,
                        Category = category,
                        Color = cat.Color
                    });
                }
            }

            _cachedCategories = categories.Values.ToList();
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
    }
}
