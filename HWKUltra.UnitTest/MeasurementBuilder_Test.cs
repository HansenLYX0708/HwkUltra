// Measurement builder tests - validates Keyence controller configuration, MeasurementRouter, and Flow nodes
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.Measurement;
using HWKUltra.Measurement.Abstractions;
using HWKUltra.Measurement.Core;
using HWKUltra.Measurement.Implementations;
using HWKUltra.Measurement.Implementations.Keyence;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Measurement.Real;

namespace HWKUltra.UnitTest
{
    public class MeasurementBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of KeyenceMeasurementControllerConfig
        /// </summary>
        public static void Test_KeyenceMeasurement_Deserialization()
        {
            Console.WriteLine("----- Measurement JSON Deserialization Validation -----");

            var json = GetTestKeyenceMeasurementJson();
            var config = JsonSerializer.Deserialize(json, MeasurementJsonContext.Default.KeyenceMeasurementControllerConfig);

            if (config == null)
                throw new Exception("KeyenceMeasurementControllerConfig deserialization failed: returned null");

            if (config.Instances == null || config.Instances.Count != 2)
                throw new Exception($"Instances count mismatch: expected 2, got {config.Instances?.Count}");

            Console.WriteLine("OK KeyenceMeasurementControllerConfig deserialization validated");
            Console.WriteLine($"  - Instances: {config.Instances.Count}");
        }

        /// <summary>
        /// Test 2: MeasurementConfig field validation
        /// </summary>
        public static void Test_MeasurementConfig_Fields()
        {
            Console.WriteLine("----- MeasurementConfig Field Validation -----");

            var json = GetTestKeyenceMeasurementJson();
            var config = JsonSerializer.Deserialize(json, MeasurementJsonContext.Default.KeyenceMeasurementControllerConfig)!;

            var main = config.Instances.Find(i => i.Name == "MainSensor");
            if (main == null) throw new Exception("Instance 'MainSensor' not found");
            if (main.DeviceId != 0) throw new Exception($"MainSensor DeviceId mismatch: expected 0, got {main.DeviceId}");
            if (main.TimeoutMs != 5000) throw new Exception($"MainSensor TimeoutMs mismatch: expected 5000, got {main.TimeoutMs}");
            if (main.DefaultSamplingCycleUs != 100) throw new Exception($"MainSensor SamplingCycle mismatch: expected 100, got {main.DefaultSamplingCycleUs}");
            if (main.DefaultFilterAverage != 4) throw new Exception($"MainSensor FilterAverage mismatch: expected 4, got {main.DefaultFilterAverage}");

            var sub = config.Instances.Find(i => i.Name == "SubSensor");
            if (sub == null) throw new Exception("Instance 'SubSensor' not found");
            if (sub.DeviceId != 1) throw new Exception($"SubSensor DeviceId mismatch: expected 1, got {sub.DeviceId}");
            if (sub.DefaultSamplingCycleUs != 200) throw new Exception($"SubSensor SamplingCycle mismatch: expected 200, got {sub.DefaultSamplingCycleUs}");
            if (sub.DefaultFilterAverage != 8) throw new Exception($"SubSensor FilterAverage mismatch: expected 8, got {sub.DefaultFilterAverage}");

            Console.WriteLine("OK MeasurementConfig fields validated");
            Console.WriteLine($"  - MainSensor: DeviceId={main.DeviceId} Sampling={main.DefaultSamplingCycleUs}us Filter={main.DefaultFilterAverage}");
            Console.WriteLine($"  - SubSensor: DeviceId={sub.DeviceId} Sampling={sub.DefaultSamplingCycleUs}us Filter={sub.DefaultFilterAverage}");
        }

        /// <summary>
        /// Test 3: Generic MeasurementBuilder builds controller and router
        /// </summary>
        public static void Test_GenericMeasurementBuilder()
        {
            Console.WriteLine("----- Generic MeasurementBuilder Test -----");

            var json = GetTestKeyenceMeasurementJson();

            var builder = new MeasurementBuilder<KeyenceMeasurementControllerConfig>(
                cfg => new KeyenceMeasurementController(cfg),
                cfg => cfg.Instances.ToDictionary(i => i.Name, i => i))
                .WithJsonDeserializer(j =>
                    JsonSerializer.Deserialize(j, MeasurementJsonContext.Default.KeyenceMeasurementControllerConfig)!);

            builder.FromJson(json);

            IMeasurementController controller = builder.BuildController();
            MeasurementRouter router = builder.BuildRouter();

            Console.WriteLine("OK Generic MeasurementBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
            Console.WriteLine($"  Router type: {router.GetType().Name}");
        }

        /// <summary>
        /// Test 4: Dedicated KeyenceMeasurementBuilder
        /// </summary>
        public static void Test_DedicatedKeyenceBuilder()
        {
            Console.WriteLine("----- Dedicated KeyenceMeasurementBuilder Test -----");

            var json = GetTestKeyenceMeasurementJson();

            var builder = new KeyenceMeasurementBuilder();
            builder.FromJson(json);

            KeyenceMeasurementController controller = builder.BuildController();
            MeasurementRouter router = builder.BuildRouter();

            Console.WriteLine("OK Dedicated KeyenceMeasurementBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
        }

        /// <summary>
        /// Test 5: Multi-instance config validation
        /// </summary>
        public static void Test_MultiInstanceConfig()
        {
            Console.WriteLine("----- Multi-Instance Config Validation -----");

            var json = GetTestKeyenceMeasurementJson();

            var builder = new KeyenceMeasurementBuilder();
            builder.FromJson(json);
            MeasurementRouter router = builder.BuildRouter();

            if (!router.HasInstance("MainSensor"))
                throw new Exception("MainSensor not found in router");
            if (!router.HasInstance("SubSensor"))
                throw new Exception("SubSensor not found in router");
            if (router.HasInstance("NonExistent"))
                throw new Exception("NonExistent should not be found in router");

            var names = router.InstanceNames;
            if (names.Count != 2)
                throw new Exception($"Expected 2 instance names, got {names.Count}");

            Console.WriteLine("OK Multi-instance config validated");
            Console.WriteLine($"  Instance names: [{string.Join(", ", names)}]");
        }

        /// <summary>
        /// Test 6: Build from KeyenceMeasurement.json file
        /// </summary>
        public static void Test_FromMeasurementJsonFile()
        {
            Console.WriteLine("----- Build from KeyenceMeasurement.json File -----");

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ConfigJson", "Measurement", "KeyenceMeasurement.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = @"g:\projects\AOIPlatform\HwkUltra_g\ConfigJson\Measurement\KeyenceMeasurement.json";
            }

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine("  Warning: KeyenceMeasurement.json not found, skipping file-based test");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            Console.WriteLine($"  Loaded KeyenceMeasurement.json ({json.Length} chars)");

            var builder = new KeyenceMeasurementBuilder();
            builder.FromJson(json);

            MeasurementRouter router = builder.BuildRouter();

            Console.WriteLine($"  OK Controller and Router built from KeyenceMeasurement.json");
            Console.WriteLine($"  Instance names: [{string.Join(", ", router.InstanceNames)}]");
        }

        /// <summary>
        /// Test 7: Measurement Flow node simulation tests
        /// </summary>
        public static async Task Test_MeasurementFlowNodes_Simulation()
        {
            Console.WriteLine("----- Measurement Flow Node Simulation Tests -----");

            var cts = new CancellationTokenSource();
            var context = new FlowContext { CancellationToken = cts.Token };

            // Test MeasurementOpenNode
            var openNode = new MeasurementOpenNode(null);
            context.Variables[$"{openNode.Id}:InstanceName"] = "MainSensor";
            var result = await openNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementOpenNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementOpenNode simulation passed");

            // Test MeasurementCloseNode
            var closeNode = new MeasurementCloseNode(null);
            context.Variables[$"{closeNode.Id}:InstanceName"] = "MainSensor";
            result = await closeNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementCloseNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementCloseNode simulation passed");

            // Test MeasurementGetDataNode
            var getDataNode = new MeasurementGetDataNode(null);
            context.Variables[$"{getDataNode.Id}:InstanceName"] = "MainSensor";
            result = await getDataNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementGetDataNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementGetDataNode simulation passed");

            // Test MeasurementStartStorageNode
            var startStorageNode = new MeasurementStartStorageNode(null);
            context.Variables[$"{startStorageNode.Id}:InstanceName"] = "MainSensor";
            result = await startStorageNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementStartStorageNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementStartStorageNode simulation passed");

            // Test MeasurementStopStorageNode
            var stopStorageNode = new MeasurementStopStorageNode(null);
            context.Variables[$"{stopStorageNode.Id}:InstanceName"] = "MainSensor";
            result = await stopStorageNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementStopStorageNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementStopStorageNode simulation passed");

            // Test MeasurementClearStorageNode
            var clearStorageNode = new MeasurementClearStorageNode(null);
            context.Variables[$"{clearStorageNode.Id}:InstanceName"] = "MainSensor";
            result = await clearStorageNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementClearStorageNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementClearStorageNode simulation passed");

            // Test MeasurementGetTrendDataNode
            var getTrendNode = new MeasurementGetTrendDataNode(null);
            context.Variables[$"{getTrendNode.Id}:InstanceName"] = "MainSensor";
            context.Variables[$"{getTrendNode.Id}:StartIndex"] = "0";
            context.Variables[$"{getTrendNode.Id}:EndIndex"] = "100";
            result = await getTrendNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementGetTrendDataNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementGetTrendDataNode simulation passed");

            // Test MeasurementSetSamplingNode
            var setSamplingNode = new MeasurementSetSamplingNode(null);
            context.Variables[$"{setSamplingNode.Id}:InstanceName"] = "MainSensor";
            context.Variables[$"{setSamplingNode.Id}:CycleUs"] = "100";
            result = await setSamplingNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementSetSamplingNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementSetSamplingNode simulation passed");

            // Test MeasurementControlNode
            var controlNode = new MeasurementControlNode(null);
            context.Variables[$"{controlNode.Id}:InstanceName"] = "MainSensor";
            context.Variables[$"{controlNode.Id}:Enable"] = "true";
            result = await controlNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"MeasurementControlNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK MeasurementControlNode simulation passed");

            Console.WriteLine("----- All Measurement Flow Node Simulation Tests Passed -----");
        }

        /// <summary>
        /// Run all tests
        /// </summary>
        public static async Task RunAllTests()
        {
            Console.WriteLine("========== MeasurementBuilder Tests Start ==========");
            Test_KeyenceMeasurement_Deserialization();
            Test_MeasurementConfig_Fields();
            Test_GenericMeasurementBuilder();
            Test_DedicatedKeyenceBuilder();
            Test_MultiInstanceConfig();
            Test_FromMeasurementJsonFile();
            await Test_MeasurementFlowNodes_Simulation();
            Console.WriteLine("========== MeasurementBuilder Tests Complete ==========");
        }

        /// <summary>
        /// Test JSON data for Keyence measurement config
        /// </summary>
        private static string GetTestKeyenceMeasurementJson()
        {
            return @"{
                ""Instances"": [
                    {
                        ""Name"": ""MainSensor"",
                        ""DeviceId"": 0,
                        ""ConnectionType"": 0,
                        ""TimeoutMs"": 5000,
                        ""DefaultSamplingCycleUs"": 100,
                        ""DefaultFilterAverage"": 4,
                        ""MaxRequestDataLength"": 512000
                    },
                    {
                        ""Name"": ""SubSensor"",
                        ""DeviceId"": 1,
                        ""ConnectionType"": 0,
                        ""TimeoutMs"": 5000,
                        ""DefaultSamplingCycleUs"": 200,
                        ""DefaultFilterAverage"": 8,
                        ""MaxRequestDataLength"": 512000
                    }
                ]
            }";
        }
    }
}
