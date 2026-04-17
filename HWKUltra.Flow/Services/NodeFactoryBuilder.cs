using HWKUltra.Builder;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Abstractions;
using HWKUltra.DeviceIO.Core;
using HWKUltra.DeviceIO.Abstractions;
using HWKUltra.LightSource.Core;
using HWKUltra.LightSource.Abstractions;
using HWKUltra.Camera.Core;
using HWKUltra.Camera.Abstractions;
using HWKUltra.AutoFocus.Core;
using HWKUltra.AutoFocus.Abstractions;
using HWKUltra.Measurement.Core;
using HWKUltra.Measurement.Abstractions;
using HWKUltra.Tray.Core;
using HWKUltra.BarcodeScanner.Core;
using HWKUltra.BarcodeScanner.Abstractions;
using HWKUltra.Communication.Core;

namespace HWKUltra.Flow.Services
{
    /// <summary>
    /// Builds a DefaultNodeFactory from optional device config JSON paths.
    /// Each device type is independently configurable — if a config file is missing or
    /// fails to load, that device category falls back to simulation (null router).
    /// </summary>
    public class NodeFactoryBuilder
    {
        /// <summary>
        /// Config JSON file paths for each device category (null = simulation)
        /// </summary>
        public string? MotionConfigPath { get; set; }
        public string? IOConfigPath { get; set; }
        public string? LightSourceConfigPath { get; set; }
        public string? CameraConfigPath { get; set; }
        public string? AutoFocusConfigPath { get; set; }
        public string? MeasurementConfigPath { get; set; }
        public string? TrayConfigPath { get; set; }
        public string? BarcodeScannerConfigPath { get; set; }
        public string? CommunicationConfigPath { get; set; }

        /// <summary>
        /// Build log: records the load status of each device category
        /// </summary>
        public List<DeviceBuildStatus> BuildLog { get; } = new();

        /// <summary>
        /// Built routers (available after Build()). Null = simulation.
        /// </summary>
        public MotionRouter? MotionRouter { get; private set; }
        public IORouter? IORouter { get; private set; }
        public LightSourceRouter? LightSourceRouter { get; private set; }
        public CameraRouter? CameraRouter { get; private set; }
        public AutoFocusRouter? AutoFocusRouter { get; private set; }
        public MeasurementRouter? MeasurementRouter { get; private set; }
        public TrayRouter? TrayRouter { get; private set; }
        public BarcodeScannerRouter? BarcodeScannerRouter { get; private set; }
        public CommunicationRouter? CommunicationRouter { get; private set; }

        /// <summary>
        /// Build a DefaultNodeFactory in pure simulation mode (all routers null)
        /// </summary>
        public DefaultNodeFactory BuildSimulated()
        {
            BuildLog.Clear();
            BuildLog.Add(new DeviceBuildStatus("All", true, "Pure simulation mode"));
            return new DefaultNodeFactory();
        }

        /// <summary>
        /// Build a DefaultNodeFactory with routers constructed from config JSON files.
        /// Any device that fails to load or has no config path falls back to simulation.
        /// </summary>
        public DefaultNodeFactory Build()
        {
            BuildLog.Clear();

            MotionRouter = TryBuildRouter("Motion", MotionConfigPath, path =>
            {
                var builder = new MotionBuilder();
                return builder.FromJsonFile(path).BuildRouter();
            });

            IORouter = TryBuildRouter("IO", IOConfigPath, path =>
            {
                var builder = new GalilIOBuilder();
                return builder.FromJsonFile(path).BuildRouter();
            });

            LightSourceRouter = TryBuildRouter("LightSource", LightSourceConfigPath, path =>
            {
                var builder = new CcsLightSourceBuilder();
                return builder.FromJsonFile(path).BuildRouter();
            });

            CameraRouter = TryBuildRouter("Camera", CameraConfigPath, path =>
            {
                var builder = new BaslerCameraBuilder();
                return builder.FromJsonFile(path).BuildRouter();
            });

            AutoFocusRouter = TryBuildRouter("AutoFocus", AutoFocusConfigPath, path =>
            {
                var builder = new LafAutoFocusBuilder();
                return builder.FromJsonFile(path).BuildRouter();
            });

            MeasurementRouter = TryBuildRouter("Measurement", MeasurementConfigPath, path =>
            {
                var json = File.ReadAllText(path);
                var builder = new KeyenceMeasurementBuilder();
                return builder.FromJson(json).BuildRouter();
            });

            TrayRouter = TryBuildRouter("Tray", TrayConfigPath, path =>
            {
                var json = File.ReadAllText(path);
                var builder = new TrayBuilder();
                return builder.FromJson(json).BuildRouter();
            });

            BarcodeScannerRouter = TryBuildRouter("BarcodeScanner", BarcodeScannerConfigPath, path =>
            {
                var json = File.ReadAllText(path);
                var builder = new BarcodeScannerBuilder();
                return builder.FromJson(json).BuildRouter();
            });

            CommunicationRouter = TryBuildRouter("Communication", CommunicationConfigPath, path =>
            {
                var json = File.ReadAllText(path);
                var builder = new WDConnectCommunicationBuilder();
                return builder.FromJson(json).BuildRouter();
            });

            return new DefaultNodeFactory(
                MotionRouter,
                IORouter,
                LightSourceRouter,
                CameraRouter,
                AutoFocusRouter,
                MeasurementRouter,
                TrayRouter,
                BarcodeScannerRouter,
                CommunicationRouter);
        }

        /// <summary>
        /// Connect/Open all real (non-null) devices. Returns a log of connect results.
        /// </summary>
        public List<string> ConnectAll()
        {
            var log = new List<string>();

            TryAction(log, "Motion", MotionRouter, r => r.Open());
            TryAction(log, "IO", IORouter, r => r.Open());
            TryAction(log, "LightSource", LightSourceRouter, r => r.Open());
            TryAction(log, "Camera", CameraRouter, r => r.Open());
            TryAction(log, "AutoFocus", AutoFocusRouter, r => r.Open());
            // Measurement/BarcodeScanner: per-instance Open, open all
            if (MeasurementRouter != null)
            {
                foreach (var name in MeasurementRouter.InstanceNames)
                    TryAction(log, $"Measurement/{name}", MeasurementRouter, r => r.Open(name));
            }
            if (BarcodeScannerRouter != null)
            {
                foreach (var name in BarcodeScannerRouter.InstanceNames)
                    TryAction(log, $"BarcodeScanner/{name}", BarcodeScannerRouter, r => r.Open(name));
            }
            // Tray: no Open/Close needed (pure data)
            if (TrayRouter != null)
                log.Add("[Tray] OK \u2014 Ready (no connection needed)");

            // Communication
            TryAction(log, "Communication", CommunicationRouter, r => r.Open());

            return log;
        }

        /// <summary>
        /// Disconnect/Close all real (non-null) devices.
        /// </summary>
        public List<string> DisconnectAll()
        {
            var log = new List<string>();

            TryAction(log, "Motion", MotionRouter, r => r.Close());
            TryAction(log, "IO", IORouter, r => r.Close());
            TryAction(log, "LightSource", LightSourceRouter, r => r.Close());
            TryAction(log, "Camera", CameraRouter, r => r.Close());
            TryAction(log, "AutoFocus", AutoFocusRouter, r => r.Close());
            if (MeasurementRouter != null)
            {
                foreach (var name in MeasurementRouter.InstanceNames)
                    TryAction(log, $"Measurement/{name}", MeasurementRouter, r => r.Close(name));
            }
            if (BarcodeScannerRouter != null)
            {
                foreach (var name in BarcodeScannerRouter.InstanceNames)
                    TryAction(log, $"BarcodeScanner/{name}", BarcodeScannerRouter, r => r.Close(name));
            }
            TryAction(log, "Communication", CommunicationRouter, r => r.Close());

            return log;
        }

        private static void TryAction<T>(List<string> log, string label, T? router, Action<T> action) where T : class
        {
            if (router == null)
            {
                log.Add($"[{label}] SKIP \u2014 Simulation");
                return;
            }
            try
            {
                action(router);
                log.Add($"[{label}] OK \u2014 Connected");
            }
            catch (Exception ex)
            {
                log.Add($"[{label}] FAIL \u2014 {ex.Message}");
            }
        }

        private T? TryBuildRouter<T>(string category, string? configPath, Func<string, T> buildFunc) where T : class
        {
            if (string.IsNullOrEmpty(configPath))
            {
                BuildLog.Add(new DeviceBuildStatus(category, true, "Simulation (no config)"));
                return null;
            }

            if (!File.Exists(configPath))
            {
                BuildLog.Add(new DeviceBuildStatus(category, true, $"Simulation (file not found: {configPath})"));
                return null;
            }

            try
            {
                var router = buildFunc(configPath);
                BuildLog.Add(new DeviceBuildStatus(category, false, $"Loaded from {Path.GetFileName(configPath)}"));
                return router;
            }
            catch (Exception ex)
            {
                BuildLog.Add(new DeviceBuildStatus(category, true, $"Simulation (load error: {ex.Message})"));
                return null;
            }
        }
    }

    /// <summary>
    /// Status record for a device category build attempt
    /// </summary>
    public class DeviceBuildStatus
    {
        public string Category { get; }
        public bool IsSimulated { get; }
        public string Message { get; }

        public DeviceBuildStatus(string category, bool isSimulated, string message)
        {
            Category = category;
            IsSimulated = isSimulated;
            Message = message;
        }

        public override string ToString() =>
            $"[{Category}] {(IsSimulated ? "SIM" : "REAL")} — {Message}";
    }
}
