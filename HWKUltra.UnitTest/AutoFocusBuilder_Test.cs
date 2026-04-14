// AutoFocus builder tests - validates LAF controller configuration, AutoFocusRouter, and Flow nodes
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.AutoFocus;
using HWKUltra.AutoFocus.Abstractions;
using HWKUltra.AutoFocus.Core;
using HWKUltra.AutoFocus.Implementations;
using HWKUltra.AutoFocus.Implementations.laf;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.AutoFocus.Real;

namespace HWKUltra.UnitTest
{
    public class AutoFocusBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of LafAutoFocusControllerConfig
        /// </summary>
        public static void Test_LafAutoFocus_Deserialization()
        {
            Console.WriteLine("----- AutoFocus JSON Deserialization Validation -----");

            var json = GetTestLafAutoFocusJson();
            var config = JsonSerializer.Deserialize(json, AutoFocusJsonContext.Default.LafAutoFocusControllerConfig);

            if (config == null)
                throw new Exception("LafAutoFocusControllerConfig deserialization failed: returned null");

            if (config.Instances == null || config.Instances.Count != 2)
                throw new Exception($"Instances count mismatch: expected 2, got {config.Instances?.Count}");

            Console.WriteLine("OK LafAutoFocusControllerConfig deserialization validated");
            Console.WriteLine($"  - Instances: {config.Instances.Count}");
        }

        /// <summary>
        /// Test 2: AutoFocusConfig field validation
        /// </summary>
        public static void Test_AutoFocusConfig_Fields()
        {
            Console.WriteLine("----- AutoFocusConfig Field Validation -----");

            var json = GetTestLafAutoFocusJson();
            var config = JsonSerializer.Deserialize(json, AutoFocusJsonContext.Default.LafAutoFocusControllerConfig)!;

            var mainAf = config.Instances.Find(i => i.Name == "MainAF");
            if (mainAf == null) throw new Exception("Instance 'MainAF' not found");
            if (mainAf.IpAddress != "127.0.0.1") throw new Exception($"MainAF IpAddress mismatch: expected '127.0.0.1', got '{mainAf.IpAddress}'");
            if (mainAf.Port != 7777) throw new Exception($"MainAF Port mismatch: expected 7777, got {mainAf.Port}");
            if (mainAf.TimeoutMs != 1000) throw new Exception($"MainAF TimeoutMs mismatch: expected 1000, got {mainAf.TimeoutMs}");

            var subAf = config.Instances.Find(i => i.Name == "SubAF");
            if (subAf == null) throw new Exception("Instance 'SubAF' not found");
            if (subAf.IpAddress != "127.0.0.1") throw new Exception($"SubAF IpAddress mismatch: expected '127.0.0.1', got '{subAf.IpAddress}'");
            if (subAf.Port != 7778) throw new Exception($"SubAF Port mismatch: expected 7778, got {subAf.Port}");
            if (subAf.TimeoutMs != 1500) throw new Exception($"SubAF TimeoutMs mismatch: expected 1500, got {subAf.TimeoutMs}");

            Console.WriteLine("OK AutoFocusConfig fields validated");
            Console.WriteLine($"  - MainAF: {mainAf.IpAddress}:{mainAf.Port} Timeout={mainAf.TimeoutMs}ms");
            Console.WriteLine($"  - SubAF: {subAf.IpAddress}:{subAf.Port} Timeout={subAf.TimeoutMs}ms");
        }

        /// <summary>
        /// Test 3: Generic AutoFocusBuilder builds controller and router
        /// </summary>
        public static void Test_GenericAutoFocusBuilder()
        {
            Console.WriteLine("----- Generic AutoFocusBuilder Test -----");

            var json = GetTestLafAutoFocusJson();

            var builder = new AutoFocusBuilder<LafAutoFocusControllerConfig>(
                cfg => new LafAutoFocusController(cfg),
                cfg => cfg.Instances.ToDictionary(i => i.Name, i => i))
                .WithJsonDeserializer(j =>
                    JsonSerializer.Deserialize(j, AutoFocusJsonContext.Default.LafAutoFocusControllerConfig)!);

            builder.FromJson(json);

            IAutoFocusController controller = builder.BuildController();
            AutoFocusRouter router = builder.BuildRouter();

            Console.WriteLine("OK Generic AutoFocusBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
            Console.WriteLine($"  Router type: {router.GetType().Name}");
        }

        /// <summary>
        /// Test 4: Dedicated LafAutoFocusBuilder
        /// </summary>
        public static void Test_DedicatedLafBuilder()
        {
            Console.WriteLine("----- Dedicated LafAutoFocusBuilder Test -----");

            var json = GetTestLafAutoFocusJson();

            var builder = new LafAutoFocusBuilder();
            builder.FromJson(json);

            LafAutoFocusController controller = builder.BuildController();
            AutoFocusRouter router = builder.BuildRouter();

            Console.WriteLine("OK Dedicated LafAutoFocusBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
        }

        /// <summary>
        /// Test 5: Multi-instance config validation
        /// </summary>
        public static void Test_MultiInstanceConfig()
        {
            Console.WriteLine("----- Multi-Instance Config Validation -----");

            var json = GetTestLafAutoFocusJson();

            var builder = new LafAutoFocusBuilder();
            builder.FromJson(json);
            AutoFocusRouter router = builder.BuildRouter();

            if (!router.HasInstance("MainAF"))
                throw new Exception("MainAF not found in router");
            if (!router.HasInstance("SubAF"))
                throw new Exception("SubAF not found in router");
            if (router.HasInstance("NonExistent"))
                throw new Exception("NonExistent should not be found in router");

            var names = router.InstanceNames;
            if (names.Count != 2)
                throw new Exception($"Expected 2 instance names, got {names.Count}");

            Console.WriteLine("OK Multi-instance config validated");
            Console.WriteLine($"  Instance names: [{string.Join(", ", names)}]");
        }

        /// <summary>
        /// Test 6: Build from LafAutoFocus.json file
        /// </summary>
        public static void Test_FromAutoFocusJsonFile()
        {
            Console.WriteLine("----- Build from LafAutoFocus.json File -----");

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ConfigJson", "AutoFocus", "LafAutoFocus.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = @"g:\projects\AOIPlatform\HwkUltra_g\ConfigJson\AutoFocus\LafAutoFocus.json";
            }

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine("  Warning: LafAutoFocus.json not found, skipping file-based test");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            Console.WriteLine($"  Loaded LafAutoFocus.json ({json.Length} chars)");

            var builder = new LafAutoFocusBuilder();
            builder.FromJson(json);

            AutoFocusRouter router = builder.BuildRouter();

            Console.WriteLine($"  OK Controller and Router built from LafAutoFocus.json");
            Console.WriteLine($"  Instance names: [{string.Join(", ", router.InstanceNames)}]");
        }

        /// <summary>
        /// Test 7: AutoFocus Flow node simulation tests
        /// </summary>
        public static async Task Test_AutoFocusFlowNodes_Simulation()
        {
            Console.WriteLine("----- AutoFocus Flow Node Simulation Tests -----");

            var cts = new CancellationTokenSource();
            var context = new FlowContext { CancellationToken = cts.Token };

            // Test AutoFocusOpenNode
            var openNode = new AutoFocusOpenNode(null);
            context.Variables[$"{openNode.Id}:InstanceName"] = "MainAF";
            var result = await openNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusOpenNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusOpenNode simulation passed");

            // Test AutoFocusCloseNode
            var closeNode = new AutoFocusCloseNode(null);
            context.Variables[$"{closeNode.Id}:InstanceName"] = "MainAF";
            result = await closeNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusCloseNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusCloseNode simulation passed");

            // Test AutoFocusEnableNode
            var enableNode = new AutoFocusEnableNode(null);
            context.Variables[$"{enableNode.Id}:InstanceName"] = "MainAF";
            result = await enableNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusEnableNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusEnableNode simulation passed");

            // Test AutoFocusDisableNode
            var disableNode = new AutoFocusDisableNode(null);
            context.Variables[$"{disableNode.Id}:InstanceName"] = "MainAF";
            result = await disableNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusDisableNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusDisableNode simulation passed");

            // Test AutoFocusLaserOnNode
            var laserOnNode = new AutoFocusLaserOnNode(null);
            context.Variables[$"{laserOnNode.Id}:InstanceName"] = "MainAF";
            result = await laserOnNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusLaserOnNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusLaserOnNode simulation passed");

            // Test AutoFocusLaserOffNode
            var laserOffNode = new AutoFocusLaserOffNode(null);
            context.Variables[$"{laserOffNode.Id}:InstanceName"] = "MainAF";
            result = await laserOffNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusLaserOffNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusLaserOffNode simulation passed");

            // Test AutoFocusGetStatusNode
            var statusNode = new AutoFocusGetStatusNode(null);
            context.Variables[$"{statusNode.Id}:InstanceName"] = "MainAF";
            result = await statusNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusGetStatusNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusGetStatusNode simulation passed");

            // Test AutoFocusCommandNode
            var commandNode = new AutoFocusCommandNode(null);
            context.Variables[$"{commandNode.Id}:InstanceName"] = "MainAF";
            context.Variables[$"{commandNode.Id}:Command"] = "st_focus";
            result = await commandNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusCommandNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusCommandNode simulation passed");

            // Test AutoFocusResetNode
            var resetNode = new AutoFocusResetNode(null);
            context.Variables[$"{resetNode.Id}:InstanceName"] = "MainAF";
            result = await resetNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"AutoFocusResetNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK AutoFocusResetNode simulation passed");

            Console.WriteLine("----- All AutoFocus Flow Node Simulation Tests Passed -----");
        }

        /// <summary>
        /// Run all tests
        /// </summary>
        public static async Task RunAllTests()
        {
            Console.WriteLine("========== AutoFocusBuilder Tests Start ==========");
            Test_LafAutoFocus_Deserialization();
            Test_AutoFocusConfig_Fields();
            Test_GenericAutoFocusBuilder();
            Test_DedicatedLafBuilder();
            Test_MultiInstanceConfig();
            Test_FromAutoFocusJsonFile();
            await Test_AutoFocusFlowNodes_Simulation();
            Console.WriteLine("========== AutoFocusBuilder Tests Complete ==========");
        }

        /// <summary>
        /// Test JSON data for LAF auto focus config
        /// </summary>
        private static string GetTestLafAutoFocusJson()
        {
            return @"{
                ""Instances"": [
                    {
                        ""Name"": ""MainAF"",
                        ""IpAddress"": ""127.0.0.1"",
                        ""Port"": 7777,
                        ""TimeoutMs"": 1000
                    },
                    {
                        ""Name"": ""SubAF"",
                        ""IpAddress"": ""127.0.0.1"",
                        ""Port"": 7778,
                        ""TimeoutMs"": 1500
                    }
                ]
            }";
        }
    }
}
