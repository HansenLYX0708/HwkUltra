// BarcodeScanner builder tests - validates config, router, and Flow nodes
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.BarcodeScanner;
using HWKUltra.BarcodeScanner.Abstractions;
using HWKUltra.BarcodeScanner.Core;
using HWKUltra.BarcodeScanner.Implementations;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.BarcodeScanner.Real;

namespace HWKUltra.UnitTest
{
    public class BarcodeScannerBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of SerialBarcodeScannerControllerConfig
        /// </summary>
        public static void Test_BarcodeScanner_Deserialization()
        {
            Console.WriteLine("----- BarcodeScanner JSON Deserialization Validation -----");

            var json = GetTestJson();
            var config = JsonSerializer.Deserialize(json, BarcodeScannerJsonContext.Default.SerialBarcodeScannerControllerConfig);

            if (config == null)
                throw new Exception("SerialBarcodeScannerControllerConfig deserialization failed: returned null");

            if (config.Instances == null || config.Instances.Count != 3)
                throw new Exception($"Instances count mismatch: expected 3, got {config.Instances?.Count}");

            Console.WriteLine("OK SerialBarcodeScannerControllerConfig deserialization validated");
            Console.WriteLine($"  - Instances: {config.Instances.Count}");
        }

        /// <summary>
        /// Test 2: BarcodeScannerConfig field validation
        /// </summary>
        public static void Test_BarcodeScannerConfig_Fields()
        {
            Console.WriteLine("----- BarcodeScannerConfig Field Validation -----");

            var json = GetTestJson();
            var config = JsonSerializer.Deserialize(json, BarcodeScannerJsonContext.Default.SerialBarcodeScannerControllerConfig)!;

            var left = config.Instances[0];
            if (left.Name != "LeftScanner") throw new Exception($"Name mismatch: expected LeftScanner, got {left.Name}");
            if (left.PortName != "COM3") throw new Exception($"PortName mismatch: expected COM3, got {left.PortName}");
            if (left.BaudRate != 9600) throw new Exception($"BaudRate mismatch: expected 9600, got {left.BaudRate}");
            if (left.DataBits != 8) throw new Exception($"DataBits mismatch: expected 8, got {left.DataBits}");
            if (left.Parity != 0) throw new Exception($"Parity mismatch: expected 0, got {left.Parity}");
            if (left.StopBits != 1) throw new Exception($"StopBits mismatch: expected 1, got {left.StopBits}");

            var handheld = config.Instances[2];
            if (handheld.Name != "HandheldScanner") throw new Exception($"Name mismatch: expected HandheldScanner, got {handheld.Name}");
            if (handheld.PortName != "COM5") throw new Exception($"PortName mismatch: expected COM5, got {handheld.PortName}");
            if (handheld.ReadTimeoutMs != 5000) throw new Exception($"ReadTimeoutMs mismatch: expected 5000, got {handheld.ReadTimeoutMs}");

            Console.WriteLine("OK BarcodeScannerConfig fields validated");
            Console.WriteLine($"  - LeftScanner: {left.PortName} @ {left.BaudRate}");
            Console.WriteLine($"  - HandheldScanner: {handheld.PortName} timeout={handheld.ReadTimeoutMs}ms");
        }

        /// <summary>
        /// Test 3: Generic BarcodeScannerBuilder
        /// </summary>
        public static void Test_GenericBuilder()
        {
            Console.WriteLine("----- Generic BarcodeScannerBuilder Validation -----");

            var builder = new BarcodeScannerBuilder<SerialBarcodeScannerControllerConfig>(
                config => new SerialBarcodeScannerController(config));
            builder.WithJsonDeserializer(json =>
                JsonSerializer.Deserialize(json, BarcodeScannerJsonContext.Default.SerialBarcodeScannerControllerConfig)!);

            var router = builder.FromJson(GetTestJson()).BuildRouter();

            if (!router.HasInstance("LeftScanner"))
                throw new Exception("Generic builder: LeftScanner instance not found");
            if (!router.HasInstance("RightScanner"))
                throw new Exception("Generic builder: RightScanner instance not found");
            if (!router.HasInstance("HandheldScanner"))
                throw new Exception("Generic builder: HandheldScanner instance not found");

            Console.WriteLine("OK Generic BarcodeScannerBuilder validated");
        }

        /// <summary>
        /// Test 4: Dedicated BarcodeScannerBuilder
        /// </summary>
        public static void Test_DedicatedBuilder()
        {
            Console.WriteLine("----- Dedicated BarcodeScannerBuilder Validation -----");

            var router = new BarcodeScannerBuilder().FromJson(GetTestJson()).BuildRouter();

            if (router.InstanceNames.Count != 3)
                throw new Exception($"Instance count mismatch: expected 3, got {router.InstanceNames.Count}");

            Console.WriteLine("OK Dedicated BarcodeScannerBuilder validated");
            Console.WriteLine($"  Instance names: [{string.Join(", ", router.InstanceNames)}]");
        }

        /// <summary>
        /// Test 5: Multi-instance support
        /// </summary>
        public static void Test_MultiInstance()
        {
            Console.WriteLine("----- Multi-Instance Validation -----");

            var router = new BarcodeScannerBuilder().FromJson(GetTestJson()).BuildRouter();

            // All instances should exist
            if (!router.HasInstance("LeftScanner") || !router.HasInstance("RightScanner") || !router.HasInstance("HandheldScanner"))
                throw new Exception("Multi-instance: missing instances");

            // All should be disconnected initially (no Open called)
            var status1 = router.GetStatus("LeftScanner");
            if (status1 != BarcodeScannerStatus.Disconnected)
                throw new Exception($"LeftScanner status mismatch: expected Disconnected, got {status1}");

            var status2 = router.GetStatus("RightScanner");
            if (status2 != BarcodeScannerStatus.Disconnected)
                throw new Exception($"RightScanner status mismatch: expected Disconnected, got {status2}");

            // GetLastBarcode should be null before any data received
            var barcode = router.GetLastBarcode("LeftScanner");
            if (barcode != null)
                throw new Exception($"LeftScanner last barcode should be null, got {barcode}");

            Console.WriteLine("OK Multi-instance validated");
        }

        /// <summary>
        /// Test 6: JSON file loading
        /// </summary>
        public static void Test_JsonFile_Loading()
        {
            Console.WriteLine("----- JSON File Loading Validation -----");

            string jsonPath = Path.Combine("ConfigJson", "BarcodeScanner", "SerialBarcodeScanner.json");
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"SKIP JSON file not found: {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize(json, BarcodeScannerJsonContext.Default.SerialBarcodeScannerControllerConfig);
            if (config == null || config.Instances == null || config.Instances.Count == 0)
                throw new Exception("Failed to load SerialBarcodeScannerControllerConfig from file");

            Console.WriteLine($"OK JSON file loaded: {config.Instances.Count} instances");
        }

        /// <summary>
        /// Test 7: Flow node simulation
        /// </summary>
        public static void Test_FlowNode_Simulation()
        {
            Console.WriteLine("----- BarcodeScanner Flow Node Simulation -----");

            var context = new FlowContext();

            // BarcodeScannerOpenNode
            var openNode = new BarcodeScannerOpenNode(null);
            context.Variables[$"{openNode.Id}:InstanceName"] = "LeftScanner";
            var result = openNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"BarcodeScannerOpenNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK BarcodeScannerOpenNode simulation passed");

            // BarcodeScannerCloseNode
            var closeNode = new BarcodeScannerCloseNode(null);
            context.Variables[$"{closeNode.Id}:InstanceName"] = "LeftScanner";
            result = closeNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"BarcodeScannerCloseNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK BarcodeScannerCloseNode simulation passed");

            // BarcodeScannerTriggerNode
            var triggerNode = new BarcodeScannerTriggerNode(null);
            context.Variables[$"{triggerNode.Id}:InstanceName"] = "LeftScanner";
            result = triggerNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"BarcodeScannerTriggerNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK BarcodeScannerTriggerNode simulation passed");

            // BarcodeScannerGetLastNode
            var getLastNode = new BarcodeScannerGetLastNode(null);
            context.Variables[$"{getLastNode.Id}:InstanceName"] = "LeftScanner";
            result = getLastNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"BarcodeScannerGetLastNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK BarcodeScannerGetLastNode simulation passed");

            Console.WriteLine("OK All BarcodeScanner Flow node simulations passed");
        }

        public static void RunAllTests()
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("  BarcodeScanner Builder Tests");
            Console.WriteLine("=======================================");
            int pass = 0, fail = 0;

            var tests = new (string Name, Action Test)[]
            {
                ("Test 1: JSON Deserialization", Test_BarcodeScanner_Deserialization),
                ("Test 2: Config Fields", Test_BarcodeScannerConfig_Fields),
                ("Test 3: Generic Builder", Test_GenericBuilder),
                ("Test 4: Dedicated Builder", Test_DedicatedBuilder),
                ("Test 5: Multi-Instance", Test_MultiInstance),
                ("Test 6: JSON File Loading", Test_JsonFile_Loading),
                ("Test 7: Flow Node Simulation", Test_FlowNode_Simulation)
            };

            foreach (var (testName, test) in tests)
            {
                try
                {
                    test();
                    pass++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAIL {testName}: {ex.Message}");
                    fail++;
                }
            }

            Console.WriteLine("=======================================");
            Console.WriteLine($"  BarcodeScanner Tests Summary: {pass} pass, {fail} fail");
            Console.WriteLine("=======================================");

            if (fail > 0)
                throw new Exception($"BarcodeScanner tests failed: {fail} test(s) failed");
        }

        private static string GetTestJson()
        {
            return """
            {
                "Instances": [
                    {
                        "Name": "LeftScanner",
                        "PortName": "COM3",
                        "BaudRate": 9600,
                        "DataBits": 8,
                        "Parity": 0,
                        "StopBits": 1,
                        "TriggerCommand": "",
                        "ReadTimeoutMs": 3000
                    },
                    {
                        "Name": "RightScanner",
                        "PortName": "COM4",
                        "BaudRate": 9600,
                        "DataBits": 8,
                        "Parity": 0,
                        "StopBits": 1,
                        "TriggerCommand": "",
                        "ReadTimeoutMs": 3000
                    },
                    {
                        "Name": "HandheldScanner",
                        "PortName": "COM5",
                        "BaudRate": 9600,
                        "DataBits": 8,
                        "Parity": 0,
                        "StopBits": 1,
                        "TriggerCommand": "",
                        "ReadTimeoutMs": 5000
                    }
                ]
            }
            """;
        }
    }
}
