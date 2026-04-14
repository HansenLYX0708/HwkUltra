// Camera builder tests - validates Basler camera controller configuration, CameraRouter, and Flow nodes
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.Camera;
using HWKUltra.Camera.Abstractions;
using HWKUltra.Camera.Core;
using HWKUltra.Camera.Implementations;
using HWKUltra.Camera.Implementations.basler;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Camera.Real;

namespace HWKUltra.UnitTest
{
    public class CameraBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of BaslerCameraControllerConfig
        /// </summary>
        public static void Test_BaslerCamera_Deserialization()
        {
            Console.WriteLine("----- Camera JSON Deserialization Validation -----");

            var json = GetTestBaslerCameraJson();
            var config = JsonSerializer.Deserialize(json, CameraJsonContext.Default.BaslerCameraControllerConfig);

            if (config == null)
                throw new Exception("BaslerCameraControllerConfig deserialization failed: returned null");

            if (config.DeviceType != "Auto")
                throw new Exception($"DeviceType mismatch: expected 'Auto', got '{config.DeviceType}'");

            if (config.MaxBufferCount != 10)
                throw new Exception($"MaxBufferCount mismatch: expected 10, got {config.MaxBufferCount}");

            if (config.Cameras == null || config.Cameras.Count != 2)
                throw new Exception($"Cameras count mismatch: expected 2, got {config.Cameras?.Count}");

            Console.WriteLine("OK BaslerCameraControllerConfig deserialization validated");
            Console.WriteLine($"  - DeviceType: {config.DeviceType}");
            Console.WriteLine($"  - MaxBufferCount: {config.MaxBufferCount}");
            Console.WriteLine($"  - Cameras: {config.Cameras.Count}");
        }

        /// <summary>
        /// Test 2: CameraConfig field validation
        /// </summary>
        public static void Test_CameraConfig_Fields()
        {
            Console.WriteLine("----- CameraConfig Field Validation -----");

            var json = GetTestBaslerCameraJson();
            var config = JsonSerializer.Deserialize(json, CameraJsonContext.Default.BaslerCameraControllerConfig)!;

            var detectCam = config.Cameras.Find(c => c.Name == "DetectCam");
            if (detectCam == null) throw new Exception("Camera 'DetectCam' not found");
            if (detectCam.SerialNumber != "40478880") throw new Exception($"DetectCam SerialNumber mismatch: expected '40478880', got '{detectCam.SerialNumber}'");
            if (detectCam.Width != 2048) throw new Exception($"DetectCam Width mismatch: expected 2048, got {detectCam.Width}");
            if (detectCam.Height != 2048) throw new Exception($"DetectCam Height mismatch: expected 2048, got {detectCam.Height}");
            if (detectCam.DefaultExposure != 50) throw new Exception($"DetectCam DefaultExposure mismatch: expected 50, got {detectCam.DefaultExposure}");
            if (detectCam.OffsetMode != 0) throw new Exception($"DetectCam OffsetMode mismatch: expected 0, got {detectCam.OffsetMode}");

            var alignCam = config.Cameras.Find(c => c.Name == "AlignCam");
            if (alignCam == null) throw new Exception("Camera 'AlignCam' not found");
            if (alignCam.SerialNumber != "40478881") throw new Exception($"AlignCam SerialNumber mismatch: expected '40478881', got '{alignCam.SerialNumber}'");
            if (alignCam.Width != 1024) throw new Exception($"AlignCam Width mismatch: expected 1024, got {alignCam.Width}");
            if (alignCam.DefaultExposure != 100) throw new Exception($"AlignCam DefaultExposure mismatch: expected 100, got {alignCam.DefaultExposure}");
            if (alignCam.DefaultGain != 5) throw new Exception($"AlignCam DefaultGain mismatch: expected 5, got {alignCam.DefaultGain}");
            if (alignCam.OffsetMode != 1) throw new Exception($"AlignCam OffsetMode mismatch: expected 1, got {alignCam.OffsetMode}");

            Console.WriteLine("OK CameraConfig fields validated");
            Console.WriteLine($"  - DetectCam: SN={detectCam.SerialNumber} {detectCam.Width}x{detectCam.Height} Exposure={detectCam.DefaultExposure}");
            Console.WriteLine($"  - AlignCam: SN={alignCam.SerialNumber} {alignCam.Width}x{alignCam.Height} Exposure={alignCam.DefaultExposure} Gain={alignCam.DefaultGain}");
        }

        /// <summary>
        /// Test 3: Generic CameraBuilder builds controller and router
        /// </summary>
        public static void Test_GenericCameraBuilder()
        {
            Console.WriteLine("----- Generic CameraBuilder Test -----");

            var json = GetTestBaslerCameraJson();

            var builder = new CameraBuilder<BaslerCameraControllerConfig>(
                cfg => new BaslerCameraController(cfg),
                cfg => cfg.Cameras.ToDictionary(c => c.Name, c => c))
                .WithJsonDeserializer(j =>
                    JsonSerializer.Deserialize(j, CameraJsonContext.Default.BaslerCameraControllerConfig)!);

            builder.FromJson(json);

            ICameraController controller = builder.BuildController();
            CameraRouter router = builder.BuildRouter();

            Console.WriteLine("OK Generic CameraBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
            Console.WriteLine($"  Router type: {router.GetType().Name}");
        }

        /// <summary>
        /// Test 4: Dedicated BaslerCameraBuilder
        /// </summary>
        public static void Test_DedicatedBaslerBuilder()
        {
            Console.WriteLine("----- Dedicated BaslerCameraBuilder Test -----");

            var json = GetTestBaslerCameraJson();

            var builder = new BaslerCameraBuilder();
            builder.FromJson(json);

            BaslerCameraController controller = builder.BuildController();
            CameraRouter router = builder.BuildRouter();

            Console.WriteLine("OK Dedicated BaslerCameraBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
        }

        /// <summary>
        /// Test 5: Multi-camera config validation
        /// </summary>
        public static void Test_MultiCameraConfig()
        {
            Console.WriteLine("----- Multi-Camera Config Validation -----");

            var json = GetTestBaslerCameraJson();

            var builder = new BaslerCameraBuilder();
            builder.FromJson(json);
            CameraRouter router = builder.BuildRouter();

            if (!router.HasCamera("DetectCam"))
                throw new Exception("DetectCam not found in router");
            if (!router.HasCamera("AlignCam"))
                throw new Exception("AlignCam not found in router");
            if (router.HasCamera("NonExistent"))
                throw new Exception("NonExistent should not be found in router");

            var names = router.CameraNames;
            if (names.Count != 2)
                throw new Exception($"Expected 2 camera names, got {names.Count}");

            Console.WriteLine("OK Multi-camera config validated");
            Console.WriteLine($"  Camera names: [{string.Join(", ", names)}]");
        }

        /// <summary>
        /// Test 6: Build from BaslerCamera.json file
        /// </summary>
        public static void Test_FromCameraJsonFile()
        {
            Console.WriteLine("----- Build from BaslerCamera.json File -----");

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ConfigJson", "Camera", "BaslerCamera.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = @"g:\projects\AOIPlatform\HwkUltra_g\ConfigJson\Camera\BaslerCamera.json";
            }

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine("  Warning: BaslerCamera.json not found, skipping file-based test");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            Console.WriteLine($"  Loaded BaslerCamera.json ({json.Length} chars)");

            var builder = new BaslerCameraBuilder();
            builder.FromJson(json);

            CameraRouter router = builder.BuildRouter();

            Console.WriteLine($"  OK Controller and Router built from BaslerCamera.json");
            Console.WriteLine($"  Camera names: [{string.Join(", ", router.CameraNames)}]");
        }

        /// <summary>
        /// Test 7: Camera Flow node simulation tests
        /// </summary>
        public static async Task Test_CameraFlowNodes_Simulation()
        {
            Console.WriteLine("----- Camera Flow Node Simulation Tests -----");

            var cts = new CancellationTokenSource();
            var context = new FlowContext { CancellationToken = cts.Token };

            // Test CameraTriggerNode
            var triggerNode = new CameraTriggerNode(null);
            context.Variables[$"{triggerNode.Id}:CameraName"] = "DetectCam";
            var result = await triggerNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CameraTriggerNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CameraTriggerNode simulation passed");

            // Test CameraOpenNode
            var openNode = new CameraOpenNode(null);
            context.Variables[$"{openNode.Id}:CameraName"] = "DetectCam";
            result = await openNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CameraOpenNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CameraOpenNode simulation passed");

            // Test CameraCloseNode
            var closeNode = new CameraCloseNode(null);
            context.Variables[$"{closeNode.Id}:CameraName"] = "DetectCam";
            result = await closeNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CameraCloseNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CameraCloseNode simulation passed");

            // Test CameraGrabNode
            var grabNode = new CameraGrabNode(null);
            context.Variables[$"{grabNode.Id}:CameraName"] = "DetectCam";
            result = await grabNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CameraGrabNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CameraGrabNode simulation passed");

            // Test CameraSetExposureNode
            var exposureNode = new CameraSetExposureNode(null);
            context.Variables[$"{exposureNode.Id}:CameraName"] = "DetectCam";
            context.Variables[$"{exposureNode.Id}:ExposureTime"] = "500";
            result = await exposureNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CameraSetExposureNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CameraSetExposureNode simulation passed");

            // Test CameraSetGainNode
            var gainNode = new CameraSetGainNode(null);
            context.Variables[$"{gainNode.Id}:CameraName"] = "DetectCam";
            context.Variables[$"{gainNode.Id}:Gain"] = "10";
            result = await gainNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CameraSetGainNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CameraSetGainNode simulation passed");

            // Test CameraSetTriggerModeNode
            var triggerModeNode = new CameraSetTriggerModeNode(null);
            context.Variables[$"{triggerModeNode.Id}:CameraName"] = "DetectCam";
            context.Variables[$"{triggerModeNode.Id}:TriggerMode"] = "Software";
            result = await triggerModeNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CameraSetTriggerModeNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CameraSetTriggerModeNode simulation passed");

            Console.WriteLine("----- All Camera Flow Node Simulation Tests Passed -----");
        }

        /// <summary>
        /// Run all tests
        /// </summary>
        public static async Task RunAllTests()
        {
            Console.WriteLine("========== CameraBuilder Tests Start ==========");
            Test_BaslerCamera_Deserialization();
            Test_CameraConfig_Fields();
            Test_GenericCameraBuilder();
            Test_DedicatedBaslerBuilder();
            Test_MultiCameraConfig();
            Test_FromCameraJsonFile();
            await Test_CameraFlowNodes_Simulation();
            Console.WriteLine("========== CameraBuilder Tests Complete ==========");
        }

        /// <summary>
        /// Test JSON data for Basler camera config
        /// </summary>
        private static string GetTestBaslerCameraJson()
        {
            return @"{
                ""DeviceType"": ""Auto"",
                ""MaxBufferCount"": 10,
                ""Cameras"": [
                    {
                        ""Name"": ""DetectCam"",
                        ""SerialNumber"": ""40478880"",
                        ""Width"": 2048,
                        ""Height"": 2048,
                        ""DefaultExposure"": 50,
                        ""DefaultGain"": 0,
                        ""OffsetMode"": 0
                    },
                    {
                        ""Name"": ""AlignCam"",
                        ""SerialNumber"": ""40478881"",
                        ""Width"": 1024,
                        ""Height"": 1024,
                        ""DefaultExposure"": 100,
                        ""DefaultGain"": 5,
                        ""OffsetMode"": 1
                    }
                ]
            }";
        }
    }
}
