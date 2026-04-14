// IOBuilder tests - validates Galil IO controller configuration and IORouter
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.DeviceIO;
using HWKUltra.DeviceIO.Abstractions;
using HWKUltra.DeviceIO.Core;
using HWKUltra.DeviceIO.Implementations;
using HWKUltra.DeviceIO.Implementations.galil;
using HWKUltra.Flow.Abstractions;

namespace HWKUltra.UnitTest
{
    public class IOBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of GalilIOConfig
        /// </summary>
        public static void Test_GalilIO_Deserialization()
        {
            Console.WriteLine("----- IO JSON Deserialization Validation -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig);

            if (config == null)
                throw new Exception("GalilIOConfig deserialization failed: returned null");

            if (config.Cards == null || config.Cards.Count != 2)
                throw new Exception($"Cards count mismatch: expected 2, got {config.Cards?.Count}");

            if (config.Cards[0].IpAddress != "192.168.1.101 -d")
                throw new Exception($"Card0 IP mismatch: expected '192.168.1.101 -d', got '{config.Cards[0].IpAddress}'");

            if (config.Cards[1].CardIndex != 1)
                throw new Exception($"Card1 CardIndex mismatch: expected 1, got {config.Cards[1].CardIndex}");

            if (config.Inputs == null || config.Inputs.Count == 0)
                throw new Exception("Inputs list is empty");

            if (config.Outputs == null || config.Outputs.Count == 0)
                throw new Exception("Outputs list is empty");

            if (config.MonitorIntervalMs != 100)
                throw new Exception($"MonitorIntervalMs mismatch: expected 100, got {config.MonitorIntervalMs}");

            Console.WriteLine("✓ GalilIOConfig deserialization validated");
            Console.WriteLine($"  - Cards: {config.Cards.Count}");
            Console.WriteLine($"  - Inputs: {config.Inputs.Count}");
            Console.WriteLine($"  - Outputs: {config.Outputs.Count}");
            Console.WriteLine($"  - MonitorIntervalMs: {config.MonitorIntervalMs}");
            Console.WriteLine($"  - DefaultOnOutputs: {config.DefaultOnOutputs.Count}");
        }

        /// <summary>
        /// Test 2: IOPointConfig field validation
        /// </summary>
        public static void Test_IOPointConfig_Fields()
        {
            Console.WriteLine("----- IOPointConfig Field Validation -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;

            // Validate a specific input point
            var emo = config.Inputs.Find(p => p.Name == "EMO");
            if (emo == null) throw new Exception("Input 'EMO' not found");
            if (emo.CardIndex != 0) throw new Exception($"EMO CardIndex mismatch: expected 0, got {emo.CardIndex}");
            if (emo.BankIndex != 0) throw new Exception($"EMO BankIndex mismatch: expected 0, got {emo.BankIndex}");
            if (emo.BitIndex != 2) throw new Exception($"EMO BitIndex mismatch: expected 2, got {emo.BitIndex}");

            // Validate a specific output point
            var cameraSwitch = config.Outputs.Find(p => p.Name == "CameraSwitch");
            if (cameraSwitch == null) throw new Exception("Output 'CameraSwitch' not found");
            if (cameraSwitch.CardIndex != 0) throw new Exception($"CameraSwitch CardIndex mismatch");
            if (cameraSwitch.BankIndex != 0) throw new Exception($"CameraSwitch BankIndex mismatch");
            if (cameraSwitch.BitIndex != 2) throw new Exception($"CameraSwitch BitIndex mismatch");

            // Validate bank1 output point
            var yCameraSwitch = config.Outputs.Find(p => p.Name == "YCameraSwitch");
            if (yCameraSwitch == null) throw new Exception("Output 'YCameraSwitch' not found");
            if (yCameraSwitch.BankIndex != 1) throw new Exception($"YCameraSwitch BankIndex mismatch: expected 1, got {yCameraSwitch.BankIndex}");
            if (yCameraSwitch.BitIndex != 0) throw new Exception($"YCameraSwitch BitIndex mismatch: expected 0, got {yCameraSwitch.BitIndex}");

            Console.WriteLine("✓ IOPointConfig fields validated");
            Console.WriteLine($"  - EMO: Card{emo.CardIndex} Bank{emo.BankIndex} Bit{emo.BitIndex}");
            Console.WriteLine($"  - CameraSwitch: Card{cameraSwitch.CardIndex} Bank{cameraSwitch.BankIndex} Bit{cameraSwitch.BitIndex}");
            Console.WriteLine($"  - YCameraSwitch: Card{yCameraSwitch.CardIndex} Bank{yCameraSwitch.BankIndex} Bit{yCameraSwitch.BitIndex}");
        }

        /// <summary>
        /// Test 3: Build IORouter using generic IOBuilder
        /// </summary>
        public static void Test_GenericIOBuilder()
        {
            Console.WriteLine("----- Generic IOBuilder Test -----");

            var json = GetTestGalilIOJson();

            var builder = new IOBuilder<GalilIOConfig>(
                cfg => new MockIOController(),
                cfg => cfg.Inputs,
                cfg => cfg.Outputs,
                cfg => cfg.MonitorIntervalMs)
                .WithJsonDeserializer(j =>
                    JsonSerializer.Deserialize(j, IOJsonContext.Default.GalilIOConfig)!);

            builder.FromJson(json);

            IIOController controller = builder.BuildController();
            IORouter router = builder.BuildRouter();

            Console.WriteLine("✓ Generic IOBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
            Console.WriteLine($"  Router input count: {router.InputNames.Count}");
            Console.WriteLine($"  Router output count: {router.OutputNames.Count}");
        }

        /// <summary>
        /// Test 4: GalilIOBuilder dedicated builder
        /// </summary>
        public static void Test_GalilIOBuilder()
        {
            Console.WriteLine("----- GalilIOBuilder Dedicated Builder Test -----");

            var builder = new GalilIOBuilder();
            Console.WriteLine("✓ GalilIOBuilder created");
        }

        /// <summary>
        /// Test 5: IORouter name-based access
        /// </summary>
        public static void Test_IORouter_NameAccess()
        {
            Console.WriteLine("----- IORouter Name-Based Access Test -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;

            var mockController = new MockIOController();
            var router = new IORouter(mockController, config.Inputs, config.Outputs, 100);

            // Verify HasInput / HasOutput
            if (!router.HasInput("EMO"))
                throw new Exception("HasInput('EMO') should be true");
            if (!router.HasOutput("CameraSwitch"))
                throw new Exception("HasOutput('CameraSwitch') should be true");
            if (router.HasInput("NonExistent"))
                throw new Exception("HasInput('NonExistent') should be false");
            if (router.HasOutput("NonExistent"))
                throw new Exception("HasOutput('NonExistent') should be false");

            // Verify input/output names
            if (router.InputNames.Count != config.Inputs.Count)
                throw new Exception($"Input count mismatch: {router.InputNames.Count} vs {config.Inputs.Count}");
            if (router.OutputNames.Count != config.Outputs.Count)
                throw new Exception($"Output count mismatch: {router.OutputNames.Count} vs {config.Outputs.Count}");

            // Test SetOutput (via mock)
            router.SetOutput("CameraSwitch", true);
            router.SetOutput("LightTowerRed", false);

            // Test GetInput / GetOutput (via mock)
            bool emoState = router.GetInput("EMO");
            bool cameraState = router.GetOutput("CameraSwitch");

            Console.WriteLine("✓ IORouter name-based access validated");
            Console.WriteLine($"  - Inputs: {router.InputNames.Count}");
            Console.WriteLine($"  - Outputs: {router.OutputNames.Count}");
            Console.WriteLine($"  - EMO state: {emoState}");
            Console.WriteLine($"  - CameraSwitch state: {cameraState}");

            // Test ReadAllInputs / ReadAllOutputs
            var allInputs = router.ReadAllInputs();
            var allOutputs = router.ReadAllOutputs();
            Console.WriteLine($"  - ReadAllInputs: {allInputs.Count} points");
            Console.WriteLine($"  - ReadAllOutputs: {allOutputs.Count} points");
        }

        /// <summary>
        /// Test 6: IORouter invalid name exception
        /// </summary>
        public static void Test_IORouter_InvalidName()
        {
            Console.WriteLine("----- IORouter Invalid Name Test -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;
            var router = new IORouter(new MockIOController(), config.Inputs, config.Outputs, 100);

            bool caughtInput = false;
            try { router.GetInput("DoesNotExist"); }
            catch (KeyNotFoundException) { caughtInput = true; }
            if (!caughtInput)
                throw new Exception("Expected KeyNotFoundException for invalid input name");

            bool caughtOutput = false;
            try { router.SetOutput("DoesNotExist", true); }
            catch (KeyNotFoundException) { caughtOutput = true; }
            if (!caughtOutput)
                throw new Exception("Expected KeyNotFoundException for invalid output name");

            Console.WriteLine("✓ IORouter invalid name exceptions validated");
        }

        /// <summary>
        /// Test 7: Load from actual GalilIO.json file
        /// </summary>
        public static void Test_FromGalilIOJsonFile()
        {
            Console.WriteLine("----- Load from GalilIO.json File -----");

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ConfigJson", "IO", "GalilIO.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = @"g:\projects\AOIPlatform\HwkUltra_g\ConfigJson\IO\GalilIO.json";
            }

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine("  ⚠ GalilIO.json not found, skipping file-based test");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            Console.WriteLine($"  Loaded GalilIO.json ({json.Length} chars)");

            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig);
            if (config == null)
                throw new Exception("Failed to deserialize GalilIO.json");

            // Validate completeness - original IOControlLib had 22 outputs and 10 inputs
            if (config.Outputs.Count < 20)
                throw new Exception($"Outputs count too low: {config.Outputs.Count}, expected >= 20 (original had 22)");
            if (config.Inputs.Count < 10)
                throw new Exception($"Inputs count too low: {config.Inputs.Count}, expected >= 10 (original had 18)");

            // Verify critical IO points exist
            var criticalOutputs = new[] {
                "LeftTrayBaseVacuum1", "RightTrayBaseVacuum1",
                "CameraSwitch", "LaserSwitch",
                "LightTowerRed", "LightTowerGreen",
                "LeftSafelyDoorLock", "RightSafelyDoorLock"
            };
            foreach (var name in criticalOutputs)
            {
                if (!config.Outputs.Exists(p => p.Name == name))
                    throw new Exception($"Critical output '{name}' missing from GalilIO.json");
            }

            var criticalInputs = new[] {
                "EMO", "VacuumPressureSensor", "CDAPressureSensor",
                "DoorSensor", "LeftLoadSwitch", "RightLoadSwitch"
            };
            foreach (var name in criticalInputs)
            {
                if (!config.Inputs.Exists(p => p.Name == name))
                    throw new Exception($"Critical input '{name}' missing from GalilIO.json");
            }

            // Verify DefaultOnOutputs
            if (config.DefaultOnOutputs.Count != 8)
                throw new Exception($"DefaultOnOutputs count mismatch: expected 8, got {config.DefaultOnOutputs.Count}");

            // Build router with mock controller to validate mapping
            var router = new IORouter(new MockIOController(), config.Inputs, config.Outputs, config.MonitorIntervalMs);

            Console.WriteLine($"  ✓ GalilIO.json validated");
            Console.WriteLine($"    Cards: {config.Cards.Count}");
            Console.WriteLine($"    Inputs: {config.Inputs.Count}");
            Console.WriteLine($"    Outputs: {config.Outputs.Count}");
            Console.WriteLine($"    DefaultOnOutputs: {config.DefaultOnOutputs.Count}");
            Console.WriteLine($"    Router inputs: {router.InputNames.Count}");
            Console.WriteLine($"    Router outputs: {router.OutputNames.Count}");
        }

        /// <summary>
        /// Test 8: IORouter monitor start/stop
        /// </summary>
        public static void Test_IORouter_Monitor()
        {
            Console.WriteLine("----- IORouter Monitor Test -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;
            var router = new IORouter(new MockIOController(), config.Inputs, config.Outputs, 50);

            int callbackCount = 0;
            router.IOStatusChanged += (inputs, outputs) =>
            {
                Interlocked.Increment(ref callbackCount);
            };

            router.StartMonitor();
            Thread.Sleep(200); // Allow a few poll cycles
            router.StopMonitor();

            if (callbackCount == 0)
                throw new Exception("IOStatusChanged was never invoked during monitoring");

            Console.WriteLine($"✓ IORouter monitor validated (callbacks received: {callbackCount})");
        }

        /// <summary>
        /// Test 9: DigitalOutputNode with IORouter
        /// </summary>
        public static void Test_DigitalOutputNode_WithRouter()
        {
            Console.WriteLine("----- DigitalOutputNode with IORouter Test -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;
            var mockController = new MockIOController();
            var router = new IORouter(mockController, config.Inputs, config.Outputs, 100);

            // Create node with IORouter
            var node = new HWKUltra.Flow.Nodes.IO.Real.DigitalOutputNode(router);
            node.Id = "test-output-node";

            // Create context with output parameters
            var context = new FlowContext();
            context.SetVariable($"{node.Id}:OutputName", "CameraSwitch");
            context.SetVariable($"{node.Id}:Value", "true");

            // Execute
            var result = node.ExecuteAsync(context).Result;
            if (!result.Success)
                throw new Exception($"DigitalOutputNode execution failed: {result.ErrorMessage}");

            // Verify output was set
            if (!mockController.WasOutputSet(0, 2)) // CameraSwitch = card0, bank0, bit2
                throw new Exception("CameraSwitch output was not set correctly");

            Console.WriteLine("DigitalOutputNode with IORouter validated");
            Console.WriteLine($"  Output 'CameraSwitch' set to ON");
        }

        /// <summary>
        /// Test 10: DigitalOutputNode pulse mode
        /// </summary>
        public static void Test_DigitalOutputNode_PulseMode()
        {
            Console.WriteLine("----- DigitalOutputNode Pulse Mode Test -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;
            var mockController = new MockIOController();
            var router = new IORouter(mockController, config.Inputs, config.Outputs, 100);

            var node = new HWKUltra.Flow.Nodes.IO.Real.DigitalOutputNode(router);
            node.Id = "test-pulse-node";

            var context = new FlowContext();
            context.SetVariable($"{node.Id}:OutputName", "LightTowerRed");
            context.SetVariable($"{node.Id}:Value", "true");
            context.SetVariable($"{node.Id}:Duration", "50"); // 50ms pulse

            var result = node.ExecuteAsync(context).Result;
            if (!result.Success)
                throw new Exception($"Pulse mode failed: {result.ErrorMessage}");

            // After pulse, output should be OFF
            if (mockController.WasOutputSet(0, 4, false) == false)
                throw new Exception("Pulse mode did not turn output OFF after duration");

            Console.WriteLine("DigitalOutputNode pulse mode validated");
        }

        /// <summary>
        /// Test 11: DigitalInputNode with IORouter
        /// </summary>
        public static void Test_DigitalInputNode_WithRouter()
        {
            Console.WriteLine("----- DigitalInputNode with IORouter Test -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;
            var mockController = new MockIOController();
            var router = new IORouter(mockController, config.Inputs, config.Outputs, 100);

            var node = new HWKUltra.Flow.Nodes.IO.Real.DigitalInputNode(router);
            node.Id = "test-input-node";

            var context = new FlowContext();
            context.SetVariable($"{node.Id}:InputName", "EMO");
            context.SetVariable($"{node.Id}:WaitForTrue", "false");

            var result = node.ExecuteAsync(context).Result;
            if (!result.Success)
                throw new Exception($"DigitalInputNode failed: {result.ErrorMessage}");

            Console.WriteLine("DigitalInputNode with IORouter validated");
            Console.WriteLine($"  Input 'EMO' read successfully");
        }

        /// <summary>
        /// Test 12: DigitalOutputNode invalid output name
        /// </summary>
        public static void Test_DigitalOutputNode_InvalidName()
        {
            Console.WriteLine("----- DigitalOutputNode Invalid Name Test -----");

            var json = GetTestGalilIOJson();
            var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;
            var mockController = new MockIOController();
            var router = new IORouter(mockController, config.Inputs, config.Outputs, 100);

            var node = new HWKUltra.Flow.Nodes.IO.Real.DigitalOutputNode(router);
            node.Id = "test-invalid-node";

            var context = new FlowContext();
            context.SetVariable($"{node.Id}:OutputName", "NonExistentOutput");
            context.SetVariable($"{node.Id}:Value", "true");

            var result = node.ExecuteAsync(context).Result;
            if (result.Success)
                throw new Exception("Expected failure for invalid output name");

            if (!result.ErrorMessage!.Contains("not found"))
                throw new Exception($"Unexpected error message: {result.ErrorMessage}");

            Console.WriteLine("DigitalOutputNode invalid name handling validated");
        }

        /// <summary>
        /// Test 13: NodeFactory creates IO nodes
        /// </summary>
        public static void Test_NodeFactory_CreatesIONodes()
        {
            Console.WriteLine("----- NodeFactory IO Node Creation Test -----");

            var factory = new HWKUltra.Flow.Services.DefaultNodeFactory();

            // Test DigitalOutput creation
            var outputNode = factory.CreateNode("DigitalOutput", new Dictionary<string, string>());
            if (outputNode == null || outputNode.NodeType != "DigitalOutput")
                throw new Exception("Failed to create DigitalOutputNode");

            // Test DigitalInput creation
            var inputNode = factory.CreateNode("DigitalInput", new Dictionary<string, string>());
            if (inputNode == null || inputNode.NodeType != "DigitalInput")
                throw new Exception("Failed to create DigitalInputNode");

            // Test legacy IoInput alias
            var legacyNode = factory.CreateNode("IoInput", new Dictionary<string, string>());
            if (legacyNode == null || legacyNode.NodeType != "DigitalInput")
                throw new Exception("Failed to create DigitalInputNode via IoInput alias");

            Console.WriteLine("NodeFactory IO node creation validated");
            Console.WriteLine($"  DigitalOutput: {outputNode.NodeType}");
            Console.WriteLine($"  DigitalInput: {inputNode.NodeType}");
            Console.WriteLine($"  IoInput alias: {legacyNode.NodeType}");
        }

        /// <summary>
        /// Run all IO builder tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========== IOBuilder Tests Start ==========");
            Test_GalilIO_Deserialization();
            Test_IOPointConfig_Fields();
            Test_GenericIOBuilder();
            Test_GalilIOBuilder();
            Test_IORouter_NameAccess();
            Test_IORouter_InvalidName();
            Test_FromGalilIOJsonFile();
            Test_IORouter_Monitor();
            // IO Node tests
            Test_DigitalOutputNode_WithRouter();
            Test_DigitalOutputNode_PulseMode();
            Test_DigitalInputNode_WithRouter();
            Test_DigitalOutputNode_InvalidName();
            Test_NodeFactory_CreatesIONodes();
            Console.WriteLine("========== IOBuilder Tests Complete ==========");
        }

        /// <summary>
        /// Inline test JSON (matches ConfigJson/IO/GalilIO.json structure)
        /// </summary>
        private static string GetTestGalilIOJson()
        {
            return @"{
                ""Cards"": [
                    { ""CardIndex"": 0, ""IpAddress"": ""192.168.1.101 -d"" },
                    { ""CardIndex"": 1, ""IpAddress"": ""192.168.1.102 -d"" }
                ],
                ""Inputs"": [
                    { ""Name"": ""LeftTrayBaseVacuum1"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 0 },
                    { ""Name"": ""LeftTrayBaseVacuum2"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 1 },
                    { ""Name"": ""LeftTrayBaseVacuum3"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 2 },
                    { ""Name"": ""LeftTrayBaseVacuum4"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 3 },
                    { ""Name"": ""RightTrayBaseVacuum1"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 4 },
                    { ""Name"": ""RightTrayBaseVacuum2"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 5 },
                    { ""Name"": ""RightTrayBaseVacuum3"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 6 },
                    { ""Name"": ""RightTrayBaseVacuum4"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 7 },
                    { ""Name"": ""VacuumPressureSensor"", ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 0 },
                    { ""Name"": ""CDAPressureSensor"",    ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 1 },
                    { ""Name"": ""EMO"",                  ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 2 },
                    { ""Name"": ""LeftLoadSwitch"",       ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 3 },
                    { ""Name"": ""RightLoadSwitch"",      ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 4 },
                    { ""Name"": ""DoorSensor"",           ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 5 },
                    { ""Name"": ""LeftSafelyDoorOpen"",   ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 6 },
                    { ""Name"": ""RightSafelyDoorOpen"",  ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 7 },
                    { ""Name"": ""LeftCylinderContract"",  ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 3 },
                    { ""Name"": ""RightCylinderContract"", ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 4 }
                ],
                ""Outputs"": [
                    { ""Name"": ""LeftTrayBaseVacuum1"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 0 },
                    { ""Name"": ""LeftTrayBaseVacuum2"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 1 },
                    { ""Name"": ""LeftTrayBaseVacuum3"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 2 },
                    { ""Name"": ""LeftTrayBaseVacuum4"",  ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 3 },
                    { ""Name"": ""RightTrayBaseVacuum1"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 4 },
                    { ""Name"": ""RightTrayBaseVacuum2"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 5 },
                    { ""Name"": ""RightTrayBaseVacuum3"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 6 },
                    { ""Name"": ""RightTrayBaseVacuum4"", ""CardIndex"": 1, ""BankIndex"": 0, ""BitIndex"": 7 },
                    { ""Name"": ""RightSafelyDoorLock"",  ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 0 },
                    { ""Name"": ""LeftSafelyDoorLock"",   ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 1 },
                    { ""Name"": ""CameraSwitch"",         ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 2 },
                    { ""Name"": ""LaserSwitch"",          ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 3 },
                    { ""Name"": ""LightTowerRed"",        ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 4 },
                    { ""Name"": ""LightTowerGreen"",      ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 5 },
                    { ""Name"": ""SideLight"",            ""CardIndex"": 0, ""BankIndex"": 0, ""BitIndex"": 6 },
                    { ""Name"": ""YCameraSwitch"",        ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 0 },
                    { ""Name"": ""YLaserSwitch"",         ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 1 },
                    { ""Name"": ""CameraXYSwitch"",       ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 2 },
                    { ""Name"": ""LaserXYSwitch"",        ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 3 },
                    { ""Name"": ""LeftCDA"",              ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 4 },
                    { ""Name"": ""RightCDA"",             ""CardIndex"": 0, ""BankIndex"": 1, ""BitIndex"": 5 }
                ],
                ""MonitorIntervalMs"": 100,
                ""DefaultOnOutputs"": [
                    ""LeftTrayBaseVacuum1"", ""LeftTrayBaseVacuum2"",
                    ""LeftTrayBaseVacuum3"", ""LeftTrayBaseVacuum4"",
                    ""RightTrayBaseVacuum1"", ""RightTrayBaseVacuum2"",
                    ""RightTrayBaseVacuum3"", ""RightTrayBaseVacuum4""
                ]
            }";
        }
    }

    /// <summary>
    /// Mock IO controller for unit testing (no real hardware required).
    /// Simulates IO bank reads by tracking SetOutput calls.
    /// </summary>
    internal class MockIOController : IIOController
    {
        private readonly Dictionary<(int card, int bit), bool> _outputs = new();
        private readonly List<(int card, int bit, bool value)> _outputHistory = new();

        public void Open() { }
        public void Close() { }

        public void SetOutput(int cardIndex, int bitIndex, bool value)
        {
            _outputs[(cardIndex, bitIndex)] = value;
            _outputHistory.Add((cardIndex, bitIndex, value));
        }

        public bool GetOutput(int cardIndex, int bitIndex)
        {
            return _outputs.TryGetValue((cardIndex, bitIndex), out var v) && v;
        }

        public bool GetInput(int cardIndex, int bitIndex)
        {
            // Simulate: all inputs off by default
            return false;
        }

        public int ReadInputBank(int cardIndex, int bankIndex)
        {
            // Simulate: all inputs off
            return 0;
        }

        public int ReadOutputBank(int cardIndex, int bankIndex)
        {
            // Reconstruct bank value from tracked outputs
            int value = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                int absoluteBit = bankIndex * 8 + bit;
                if (_outputs.TryGetValue((cardIndex, absoluteBit), out var v) && v)
                {
                    value |= (1 << bit);
                }
            }
            return value;
        }

        /// <summary>
        /// Check if output was set to true at any point
        /// </summary>
        public bool WasOutputSet(int cardIndex, int bitIndex)
        {
            return _outputHistory.Any(h => h.card == cardIndex && h.bit == bitIndex && h.value);
        }

        /// <summary>
        /// Check if output was set to specific value
        /// </summary>
        public bool WasOutputSet(int cardIndex, int bitIndex, bool value)
        {
            return _outputHistory.Any(h => h.card == cardIndex && h.bit == bitIndex && h.value == value);
        }
    }
}
