// Generic builder tests - validates both Elmo and GTS controllers
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.Motion;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations.elmo;
using HWKUltra.Motion.Implementations.gts;
using System.IO;

namespace HWKUltra.UnitTest
{
    public class MotionBuilderTest
    {
        /// <summary>
        /// Test 1: Build Elmo controller using generic builder
        /// </summary>
        public static void Test_Elmo_GenericBuilder()
        {
            var elmoJson = @"{
                ""TargetIP"": ""192.168.1.100"",
                ""TargetPort"": 502,
                ""LocalIP"": ""192.168.1.10"",
                ""LocalPort"": 503,
                ""Mask"": 4294967295,
                ""CAMPointsCount"": 500,
                ""SDODelay"": 50,
                ""SDOTimeout"": 1000,
                ""OCTriggerDuring"": 100,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""DriverName"": ""LX"",
                        ""PulsePerUnit"": 10000.0
                    },
                    {
                        ""Name"": ""Y"",
                        ""DriverName"": ""LY"",
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""DriverName"": ""LXY"",
                        ""Axes"": [""X"", ""Y""]
                    }
                ]
            }";

            // Directly use generic builder, configure source generator for deserialization
            var elmoBuilder = new MotionBuilder<ElmoMotionControllerConfig>(
                cfg => new ElmoMotionController(cfg),
                cfg => cfg.Axes.Select((axis, index) => new { axis.Name, Index = index })
                              .ToDictionary(x => x.Name, x => x.Index))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.ElmoMotionControllerConfig)!);

            elmoBuilder.FromJson(elmoJson);

            IMotionController controller = elmoBuilder.BuildController();
            MotionRouter router = elmoBuilder.BuildRouter();

            Console.WriteLine("✓ Elmo generic builder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
        }

        /// <summary>
        /// Test 2: Build GTS controller using generic builder
        /// </summary>
        public static void Test_GTS_GenericBuilder()
        {
            var gtsJson = @"{
                ""CardId"": 0,
                ""ConfigFilePath"": ""gts.cfg"",
                ""DefaultVel"": 100000,
                ""DefaultAcc"": 1000000,
                ""DefaultDec"": 1000000,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""AxisId"": 1,
                        ""PulsePerUnit"": 10000.0
                    },
                    {
                        ""Name"": ""Y"",
                        ""AxisId"": 2,
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""CrdId"": 1,
                        ""Axes"": [""X"", ""Y""],
                        ""Dimension"": 2
                    }
                ],
                ""CrdParams"": [
                    {
                        ""CrdId"": 1,
                        ""Dimension"": 2,
                        ""LeadAxis"": 1,
                        ""Axes"": [1, 2]
                    }
                ]
            }";

            // Directly use generic builder, configure source generator for deserialization
            var gtsBuilder = new MotionBuilder<GtsMotionControllerConfig>(
                cfg => new GtsMotionController(cfg),
                cfg => cfg.Axes.ToDictionary(x => x.Name, x => (int)x.AxisId))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.GtsMotionControllerConfig)!);

            gtsBuilder.FromJson(gtsJson);

            IMotionController controller = gtsBuilder.BuildController();
            MotionRouter router = gtsBuilder.BuildRouter();

            Console.WriteLine("✓ GTS generic builder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
        }

        /// <summary>
        /// Test 3: Dedicated builders (syntax sugar)
        /// </summary>
        public static void Test_DedicatedBuilders()
        {
            // Elmo dedicated builder
            var elmoBuilder = new MotionBuilder();  // Backward compatible
            Console.WriteLine("✓ Elmo dedicated builder created");

            // GTS dedicated builder
            var gtsBuilder = new GtsMotionBuilder();
            Console.WriteLine("✓ GTS dedicated builder created");
        }

        /// <summary>
        /// Test 4: Unified interface handling
        /// </summary>
        public static void Test_UnifiedInterface()
        {
            // Create both controller types
            var elmoController = CreateElmoController();
            var gtsController = CreateGtsController();

            // Use through unified interface
            TestController(elmoController, "Elmo");
            TestController(gtsController, "GTS");
        }

        private static IMotionController CreateElmoController()
        {
            var builder = new MotionBuilder<ElmoMotionControllerConfig>(
                cfg => new ElmoMotionController(cfg),
                cfg => cfg.Axes.Select((axis, index) => new { axis.Name, Index = index })
                              .ToDictionary(x => x.Name, x => x.Index))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.ElmoMotionControllerConfig)!);

            var json = @"{""TargetIP"": ""192.168.1.100"",""TargetPort"": 502,""LocalIP"": ""192.168.1.10"",""LocalPort"": 503,""Mask"": 4294967295,""CAMPointsCount"": 500,""SDODelay"": 50,""SDOTimeout"": 1000,""OCTriggerDuring"": 100,""Axes"": [{""Name"": ""X"",""DriverName"": ""LX"",""PulsePerUnit"": 10000.0}],""Groups"": []}";
            builder.FromJson(json);
            return builder.BuildController();
        }

        private static IMotionController CreateGtsController()
        {
            var builder = new MotionBuilder<GtsMotionControllerConfig>(
                cfg => new GtsMotionController(cfg),
                cfg => cfg.Axes.ToDictionary(x => x.Name, x => (int)x.AxisId))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.GtsMotionControllerConfig)!);

            var json = @"{""CardId"": 0,""Axes"": [{""Name"": ""X"",""AxisId"": 1,""PulsePerUnit"": 10000.0}],""Groups"": [],""CrdParams"": []}";
            builder.FromJson(json);
            return builder.BuildController();
        }

        private static void TestController(IMotionController controller, string name)
        {
            Console.WriteLine($"[{name}] Controller: {controller.GetType().Name}");
            // Unified interface calls
            // controller.Open();
            // controller.MoveAxis("X", 100.0);
            // controller.Close();
        }

        /// <summary>
        /// Test 5: Validate JSON deserialization details
        /// </summary>
        public static void Test_DeserializationValidation()
        {
            Console.WriteLine("----- JSON Deserialization Validation -----");

            // Test Elmo config deserialization
            var elmoJson = @"{
                ""TargetIP"": ""192.168.1.100"",
                ""TargetPort"": 502,
                ""LocalIP"": ""192.168.1.10"",
                ""LocalPort"": 503,
                ""Mask"": 4294967295,
                ""CAMPointsCount"": 500,
                ""SDODelay"": 50,
                ""SDOTimeout"": 1000,
                ""OCTriggerDuring"": 100,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""DriverName"": ""LX"",
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""DriverName"": ""LXY"",
                        ""Axes"": [""X"", ""Y""]
                    }
                ]
            }";

            var elmoConfig = JsonSerializer.Deserialize(elmoJson, MotionJsonContext.Default.ElmoMotionControllerConfig);

            if (elmoConfig == null)
                throw new Exception("Elmo config deserialization failed: returned null");

            if (elmoConfig.TargetIP != "192.168.1.100")
                throw new Exception($"TargetIP mismatch: expected '192.168.1.100', got '{elmoConfig.TargetIP}'");

            if (elmoConfig.TargetPort != 502)
                throw new Exception($"TargetPort mismatch: expected 502, got {elmoConfig.TargetPort}");

            if (elmoConfig.Axes == null || elmoConfig.Axes.Count == 0)
                throw new Exception("Axes list is empty or not properly deserialized");

            if (elmoConfig.Axes[0].Name != "X")
                throw new Exception($"Axis name mismatch: expected 'X', got '{elmoConfig.Axes[0].Name}'");

            if (elmoConfig.Axes[0].DriverName != "LX")
                throw new Exception($"DriverName mismatch: expected 'LX', got '{elmoConfig.Axes[0].DriverName}'");

            if (elmoConfig.Groups == null || elmoConfig.Groups.Count == 0)
                throw new Exception("Groups list is empty or not properly deserialized");

            if (elmoConfig.Groups[0].Name != "XY")
                throw new Exception($"Group name mismatch: expected 'XY', got '{elmoConfig.Groups[0].Name}'");

            Console.WriteLine("✓ Elmo config deserialization validated");
            Console.WriteLine($"  - TargetIP: {elmoConfig.TargetIP}");
            Console.WriteLine($"  - Axes count: {elmoConfig.Axes.Count}");
            Console.WriteLine($"  - Groups count: {elmoConfig.Groups.Count}");

            // Test GTS config deserialization
            var gtsJson = @"{
                ""CardId"": 0,
                ""ConfigFilePath"": ""gts.cfg"",
                ""DefaultVel"": 100000,
                ""DefaultAcc"": 1000000,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""AxisId"": 1,
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""CrdId"": 1,
                        ""Axes"": [""X""],
                        ""Dimension"": 2
                    }
                ],
                ""CrdParams"": [
                    {
                        ""CrdId"": 1,
                        ""Dimension"": 2,
                        ""Axes"": [1]
                    }
                ]
            }";

            var gtsConfig = JsonSerializer.Deserialize(gtsJson, MotionJsonContext.Default.GtsMotionControllerConfig);

            if (gtsConfig == null)
                throw new Exception("GTS config deserialization failed: returned null");

            if (gtsConfig.CardId != 0)
                throw new Exception($"CardId mismatch: expected 0, got {gtsConfig.CardId}");

            if (gtsConfig.ConfigFilePath != "gts.cfg")
                throw new Exception($"ConfigFilePath mismatch: expected 'gts.cfg', got '{gtsConfig.ConfigFilePath}'");

            if (gtsConfig.Axes == null || gtsConfig.Axes.Count == 0)
                throw new Exception("GTS Axes list is empty");

            if (gtsConfig.Axes[0].AxisId != 1)
                throw new Exception($"AxisId mismatch: expected 1, got {gtsConfig.Axes[0].AxisId}");

            if (gtsConfig.CrdParams == null || gtsConfig.CrdParams.Count == 0)
                throw new Exception("CrdParams list is empty");

            Console.WriteLine("✓ GTS config deserialization validated");
            Console.WriteLine($"  - CardId: {gtsConfig.CardId}");
            Console.WriteLine($"  - ConfigFilePath: {gtsConfig.ConfigFilePath}");
            Console.WriteLine($"  - Axes count: {gtsConfig.Axes.Count}");
            Console.WriteLine($"  - CrdParams count: {gtsConfig.CrdParams.Count}");

            Console.WriteLine("----- JSON Deserialization Validation Complete -----");
        }

        /// <summary>
        /// Test 6: Build Elmo controller from actual Motion.json file
        /// </summary>
        public static void Test_FromMotionJsonFile()
        {
            Console.WriteLine("----- Build from Motion.json File -----");

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ConfigJson", "Motion", "ElmoMotion.json");
            if (!File.Exists(jsonPath))
            {
                // Try source tree path
                jsonPath = @"g:\projects\AOIPlatform\HwkUltra_g\ConfigJson\Motion\ElmoMotion.json";
            }

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine("  ⚠ Motion.json not found, skipping file-based test");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            Console.WriteLine($"  Loaded ElmoMotion.json ({json.Length} chars)");

            var builder = new MotionBuilder<ElmoMotionControllerConfig>(
                cfg => new ElmoMotionController(cfg),
                cfg => cfg.Axes.Select((axis, index) => new { axis.Name, Index = index })
                              .ToDictionary(x => x.Name, x => x.Index))
                .WithJsonDeserializer(j =>
                    JsonSerializer.Deserialize(j, MotionJsonContext.Default.ElmoMotionControllerConfig)!);

            builder.FromJson(json);

            IMotionController controller = builder.BuildController();
            MotionRouter router = builder.BuildRouter();

            Console.WriteLine($"  ✓ Controller built from Motion.json: {controller.GetType().Name}");
            Console.WriteLine($"  ✓ Router built from Motion.json: {router.GetType().Name}");
        }

        /// <summary>
        /// Run all tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========== MotionBuilder Tests Start ==========");
            Test_DeserializationValidation();
            Test_Elmo_GenericBuilder();
            Test_GTS_GenericBuilder();
            Test_DedicatedBuilders();
            Test_UnifiedInterface();
            Test_FromMotionJsonFile();
            Console.WriteLine("========== MotionBuilder Tests Complete ==========");
        }
    }
}
