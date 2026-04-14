// LightSource builder tests - validates CCS light source controller configuration and LightSourceRouter
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.LightSource;
using HWKUltra.LightSource.Abstractions;
using HWKUltra.LightSource.Core;
using HWKUltra.LightSource.Implementations;
using HWKUltra.LightSource.Implementations.ccs;
using HWKUltra.Flow.Abstractions;

namespace HWKUltra.UnitTest
{
    public class LightSourceBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of CcsLightSourceControllerConfig
        /// </summary>
        public static void Test_CcsLightSource_Deserialization()
        {
            Console.WriteLine("----- LightSource JSON Deserialization Validation -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig);

            if (config == null)
                throw new Exception("CcsLightSourceControllerConfig deserialization failed: returned null");

            if (config.IpAddress != "192.168.1.80")
                throw new Exception($"IpAddress mismatch: expected '192.168.1.80', got '{config.IpAddress}'");

            if (config.Port != 40001)
                throw new Exception($"Port mismatch: expected 40001, got {config.Port}");

            if (config.ConnectionTimeoutMs != 3000)
                throw new Exception($"ConnectionTimeoutMs mismatch: expected 3000, got {config.ConnectionTimeoutMs}");

            if (config.CommandDelayMs != 100)
                throw new Exception($"CommandDelayMs mismatch: expected 100, got {config.CommandDelayMs}");

            if (config.Channels == null || config.Channels.Count != 4)
                throw new Exception($"Channels count mismatch: expected 4, got {config.Channels?.Count}");

            Console.WriteLine("OK CcsLightSourceControllerConfig deserialization validated");
            Console.WriteLine($"  - IpAddress: {config.IpAddress}");
            Console.WriteLine($"  - Port: {config.Port}");
            Console.WriteLine($"  - Channels: {config.Channels.Count}");
        }

        /// <summary>
        /// Test 2: LightChannelConfig field validation
        /// </summary>
        public static void Test_LightChannelConfig_Fields()
        {
            Console.WriteLine("----- LightChannelConfig Field Validation -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;

            var topLight = config.Channels.Find(c => c.Name == "TopLight");
            if (topLight == null) throw new Exception("Channel 'TopLight' not found");
            if (topLight.ChannelIndex != 0) throw new Exception($"TopLight ChannelIndex mismatch: expected 0, got {topLight.ChannelIndex}");
            if (topLight.DefaultIntensity != 512) throw new Exception($"TopLight DefaultIntensity mismatch: expected 512, got {topLight.DefaultIntensity}");
            if (topLight.MaxIntensity != 1023) throw new Exception($"TopLight MaxIntensity mismatch: expected 1023, got {topLight.MaxIntensity}");

            var sideLight = config.Channels.Find(c => c.Name == "SideLight");
            if (sideLight == null) throw new Exception("Channel 'SideLight' not found");
            if (sideLight.ChannelIndex != 2) throw new Exception($"SideLight ChannelIndex mismatch: expected 2, got {sideLight.ChannelIndex}");

            Console.WriteLine("OK LightChannelConfig fields validated");
            Console.WriteLine($"  - TopLight: Channel{topLight.ChannelIndex} DefaultIntensity={topLight.DefaultIntensity} MaxIntensity={topLight.MaxIntensity}");
            Console.WriteLine($"  - SideLight: Channel{sideLight.ChannelIndex} DefaultIntensity={sideLight.DefaultIntensity}");
        }

        /// <summary>
        /// Test 3: Build LightSourceRouter using generic builder
        /// </summary>
        public static void Test_GenericLightSourceBuilder()
        {
            Console.WriteLine("----- Generic LightSourceBuilder Test -----");

            var json = GetTestCcsLightSourceJson();

            var builder = new LightSourceBuilder<CcsLightSourceControllerConfig>(
                cfg => new MockLightSourceController(),
                cfg => cfg.Channels.ToDictionary(c => c.Name, c => c))
                .WithJsonDeserializer(j =>
                    JsonSerializer.Deserialize(j, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!);

            builder.FromJson(json);

            ILightSourceController controller = builder.BuildController();
            LightSourceRouter router = builder.BuildRouter();

            Console.WriteLine("OK Generic LightSourceBuilder test passed");
            Console.WriteLine($"  Controller type: {controller.GetType().Name}");
            Console.WriteLine($"  Router channel count: {router.ChannelNames.Count}");
        }

        /// <summary>
        /// Test 4: CcsLightSourceBuilder dedicated builder
        /// </summary>
        public static void Test_CcsLightSourceBuilder()
        {
            Console.WriteLine("----- CcsLightSourceBuilder Dedicated Builder Test -----");

            var builder = new CcsLightSourceBuilder();
            Console.WriteLine("OK CcsLightSourceBuilder created");
        }

        /// <summary>
        /// Test 5: LightSourceRouter name-based access
        /// </summary>
        public static void Test_LightSourceRouter_NameAccess()
        {
            Console.WriteLine("----- LightSourceRouter Name-Based Access Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;

            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            // Verify HasChannel
            if (!router.HasChannel("TopLight"))
                throw new Exception("HasChannel('TopLight') should be true");
            if (!router.HasChannel("BottomLight"))
                throw new Exception("HasChannel('BottomLight') should be true");
            if (router.HasChannel("NonExistent"))
                throw new Exception("HasChannel('NonExistent') should be false");

            // Verify channel names
            if (router.ChannelNames.Count != config.Channels.Count)
                throw new Exception($"Channel count mismatch: {router.ChannelNames.Count} vs {config.Channels.Count}");

            // Test TurnOn / TurnOff
            router.TurnOn("TopLight");
            if (!mockController.IsOn(0))
                throw new Exception("TopLight should be on after TurnOn");

            router.TurnOff("TopLight");
            if (mockController.IsOn(0))
                throw new Exception("TopLight should be off after TurnOff");

            // Test SetIntensity
            router.SetIntensity("TopLight", 800);
            if (mockController.GetIntensity(0) != 800)
                throw new Exception($"TopLight intensity mismatch: expected 800, got {mockController.GetIntensity(0)}");

            Console.WriteLine("OK LightSourceRouter name-based access validated");
            Console.WriteLine($"  - Channels: {router.ChannelNames.Count}");
        }

        /// <summary>
        /// Test 6: LightSourceRouter SetTriggerMode composite operation
        /// </summary>
        public static void Test_LightSourceRouter_TriggerMode()
        {
            Console.WriteLine("----- LightSourceRouter SetTriggerMode Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;

            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            router.SetTriggerMode("TopLight", 700);

            // After trigger mode: channel should be on, intensity set, pulse mode external
            if (!mockController.IsOn(0))
                throw new Exception("TopLight should be on after SetTriggerMode");
            if (mockController.GetIntensity(0) != 700)
                throw new Exception($"TopLight intensity mismatch: expected 700, got {mockController.GetIntensity(0)}");
            if (mockController.GetPulseMode(0) != LightPulseMode.External)
                throw new Exception($"TopLight pulse mode mismatch: expected External, got {mockController.GetPulseMode(0)}");

            Console.WriteLine("OK LightSourceRouter SetTriggerMode validated");
            Console.WriteLine($"  - Channel on: {mockController.IsOn(0)}");
            Console.WriteLine($"  - Intensity: {mockController.GetIntensity(0)}");
            Console.WriteLine($"  - PulseMode: {mockController.GetPulseMode(0)}");
        }

        /// <summary>
        /// Test 7: LightSourceRouter SetContinuousMode composite operation
        /// </summary>
        public static void Test_LightSourceRouter_ContinuousMode()
        {
            Console.WriteLine("----- LightSourceRouter SetContinuousMode Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;

            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            router.SetContinuousMode("BottomLight", 1);

            // After continuous mode: channel should be on, low intensity, pulse mode off
            if (!mockController.IsOn(1))
                throw new Exception("BottomLight should be on after SetContinuousMode");
            if (mockController.GetIntensity(1) != 1)
                throw new Exception($"BottomLight intensity mismatch: expected 1, got {mockController.GetIntensity(1)}");
            if (mockController.GetPulseMode(1) != LightPulseMode.Off)
                throw new Exception($"BottomLight pulse mode mismatch: expected Off, got {mockController.GetPulseMode(1)}");

            Console.WriteLine("OK LightSourceRouter SetContinuousMode validated");
            Console.WriteLine($"  - Channel on: {mockController.IsOn(1)}");
            Console.WriteLine($"  - Intensity: {mockController.GetIntensity(1)}");
            Console.WriteLine($"  - PulseMode: {mockController.GetPulseMode(1)}");
        }

        /// <summary>
        /// Test 8: LightSourceRouter invalid channel exception
        /// </summary>
        public static void Test_LightSourceRouter_InvalidChannel()
        {
            Console.WriteLine("----- LightSourceRouter Invalid Channel Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;

            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            bool caughtException = false;
            try { router.TurnOn("DoesNotExist"); }
            catch (HWKUltra.Core.LightSourceException) { caughtException = true; }
            if (!caughtException)
                throw new Exception("Expected LightSourceException for invalid channel name");

            Console.WriteLine("OK LightSourceRouter invalid channel exception validated");
        }

        /// <summary>
        /// Test 9: Load from actual CcsLightSource.json file
        /// </summary>
        public static void Test_FromCcsLightSourceJsonFile()
        {
            Console.WriteLine("----- Load from CcsLightSource.json File -----");

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ConfigJson", "LightSource", "CcsLightSource.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = @"g:\projects\AOIPlatform\HwkUltra_g\ConfigJson\LightSource\CcsLightSource.json";
            }

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine("  WARNING CcsLightSource.json not found, skipping file-based test");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            Console.WriteLine($"  Loaded CcsLightSource.json ({json.Length} chars)");

            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig);
            if (config == null)
                throw new Exception("Failed to deserialize CcsLightSource.json");

            if (config.Channels.Count < 1)
                throw new Exception($"Channels count too low: {config.Channels.Count}");

            // Build router with mock controller to validate mapping
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(new MockLightSourceController(), channelMap);

            Console.WriteLine($"  OK CcsLightSource.json validated");
            Console.WriteLine($"    IpAddress: {config.IpAddress}");
            Console.WriteLine($"    Port: {config.Port}");
            Console.WriteLine($"    Channels: {config.Channels.Count}");
            Console.WriteLine($"    Router channels: {router.ChannelNames.Count}");
        }

        /// <summary>
        /// Test 10: LightSetTriggerModeNode with LightSourceRouter
        /// </summary>
        public static void Test_LightSetTriggerModeNode_WithRouter()
        {
            Console.WriteLine("----- LightSetTriggerModeNode with LightSourceRouter Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;
            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            var node = new HWKUltra.Flow.Nodes.LightSource.Real.LightSetTriggerModeNode(router);
            node.Id = "test-trigger-node";

            var context = new FlowContext();
            context.SetVariable($"{node.Id}:ChannelName", "TopLight");
            context.SetVariable($"{node.Id}:Intensity", "700");

            var result = node.ExecuteAsync(context).Result;
            if (!result.Success)
                throw new Exception($"LightSetTriggerModeNode execution failed: {result.ErrorMessage}");

            if (!mockController.IsOn(0))
                throw new Exception("TopLight should be on after trigger mode node");
            if (mockController.GetPulseMode(0) != LightPulseMode.External)
                throw new Exception("TopLight pulse mode should be External");

            Console.WriteLine("OK LightSetTriggerModeNode with LightSourceRouter validated");
        }

        /// <summary>
        /// Test 11: LightSetContinuousModeNode with LightSourceRouter
        /// </summary>
        public static void Test_LightSetContinuousModeNode_WithRouter()
        {
            Console.WriteLine("----- LightSetContinuousModeNode with LightSourceRouter Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;
            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            var node = new HWKUltra.Flow.Nodes.LightSource.Real.LightSetContinuousModeNode(router);
            node.Id = "test-continuous-node";

            var context = new FlowContext();
            context.SetVariable($"{node.Id}:ChannelName", "BottomLight");
            context.SetVariable($"{node.Id}:Intensity", "1");

            var result = node.ExecuteAsync(context).Result;
            if (!result.Success)
                throw new Exception($"LightSetContinuousModeNode execution failed: {result.ErrorMessage}");

            if (!mockController.IsOn(1))
                throw new Exception("BottomLight should be on after continuous mode node");
            if (mockController.GetPulseMode(1) != LightPulseMode.Off)
                throw new Exception("BottomLight pulse mode should be Off");

            Console.WriteLine("OK LightSetContinuousModeNode with LightSourceRouter validated");
        }

        /// <summary>
        /// Test 12: LightTurnOnOffNode with LightSourceRouter
        /// </summary>
        public static void Test_LightTurnOnOffNode_WithRouter()
        {
            Console.WriteLine("----- LightTurnOnOffNode with LightSourceRouter Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;
            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            var node = new HWKUltra.Flow.Nodes.LightSource.Real.LightTurnOnOffNode(router);
            node.Id = "test-onoff-node";

            // Test turn on
            var context = new FlowContext();
            context.SetVariable($"{node.Id}:ChannelName", "SideLight");
            context.SetVariable($"{node.Id}:Value", "true");

            var result = node.ExecuteAsync(context).Result;
            if (!result.Success)
                throw new Exception($"LightTurnOnOffNode ON failed: {result.ErrorMessage}");
            if (!mockController.IsOn(2))
                throw new Exception("SideLight should be on");

            // Test turn off
            var context2 = new FlowContext();
            context2.SetVariable($"{node.Id}:ChannelName", "SideLight");
            context2.SetVariable($"{node.Id}:Value", "false");

            var result2 = node.ExecuteAsync(context2).Result;
            if (!result2.Success)
                throw new Exception($"LightTurnOnOffNode OFF failed: {result2.ErrorMessage}");
            if (mockController.IsOn(2))
                throw new Exception("SideLight should be off");

            Console.WriteLine("OK LightTurnOnOffNode with LightSourceRouter validated");
        }

        /// <summary>
        /// Test 13: LightTurnOnOffNode invalid channel name
        /// </summary>
        public static void Test_LightTurnOnOffNode_InvalidChannel()
        {
            Console.WriteLine("----- LightTurnOnOffNode Invalid Channel Test -----");

            var json = GetTestCcsLightSourceJson();
            var config = JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!;
            var mockController = new MockLightSourceController();
            var channelMap = config.Channels.ToDictionary(c => c.Name, c => c);
            var router = new LightSourceRouter(mockController, channelMap);

            var node = new HWKUltra.Flow.Nodes.LightSource.Real.LightTurnOnOffNode(router);
            node.Id = "test-invalid-node";

            var context = new FlowContext();
            context.SetVariable($"{node.Id}:ChannelName", "NonExistentLight");
            context.SetVariable($"{node.Id}:Value", "true");

            var result = node.ExecuteAsync(context).Result;
            if (result.Success)
                throw new Exception("Expected failure for invalid channel name");
            if (!result.ErrorMessage!.Contains("not found"))
                throw new Exception($"Unexpected error message: {result.ErrorMessage}");

            Console.WriteLine("OK LightTurnOnOffNode invalid channel handling validated");
        }

        /// <summary>
        /// Test 14: NodeFactory creates LightSource nodes
        /// </summary>
        public static void Test_NodeFactory_CreatesLightSourceNodes()
        {
            Console.WriteLine("----- NodeFactory LightSource Node Creation Test -----");

            var factory = new HWKUltra.Flow.Services.DefaultNodeFactory();

            // Test LightSetTriggerMode creation
            var triggerNode = factory.CreateNode("LightSetTriggerMode", new Dictionary<string, string>());
            if (triggerNode == null || triggerNode.NodeType != "LightSetTriggerMode")
                throw new Exception("Failed to create LightSetTriggerModeNode");

            // Test LightSetContinuousMode creation
            var continuousNode = factory.CreateNode("LightSetContinuousMode", new Dictionary<string, string>());
            if (continuousNode == null || continuousNode.NodeType != "LightSetContinuousMode")
                throw new Exception("Failed to create LightSetContinuousModeNode");

            // Test LightTurnOnOff creation
            var onoffNode = factory.CreateNode("LightTurnOnOff", new Dictionary<string, string>());
            if (onoffNode == null || onoffNode.NodeType != "LightTurnOnOff")
                throw new Exception("Failed to create LightTurnOnOffNode");

            // Test alias: LightTrigger
            var aliasNode = factory.CreateNode("LightTrigger", new Dictionary<string, string>());
            if (aliasNode == null || aliasNode.NodeType != "LightSetTriggerMode")
                throw new Exception("Failed to create LightSetTriggerModeNode via LightTrigger alias");

            // Test alias: LightSwitch
            var switchNode = factory.CreateNode("LightSwitch", new Dictionary<string, string>());
            if (switchNode == null || switchNode.NodeType != "LightTurnOnOff")
                throw new Exception("Failed to create LightTurnOnOffNode via LightSwitch alias");

            Console.WriteLine("OK NodeFactory LightSource node creation validated");
            Console.WriteLine($"  LightSetTriggerMode: {triggerNode.NodeType}");
            Console.WriteLine($"  LightSetContinuousMode: {continuousNode.NodeType}");
            Console.WriteLine($"  LightTurnOnOff: {onoffNode.NodeType}");
            Console.WriteLine($"  LightTrigger alias: {aliasNode.NodeType}");
            Console.WriteLine($"  LightSwitch alias: {switchNode.NodeType}");
        }

        /// <summary>
        /// Test 15: Simulation mode (null router)
        /// </summary>
        public static void Test_LightSourceNodes_Simulation()
        {
            Console.WriteLine("----- LightSource Nodes Simulation Test -----");

            // Create nodes without router (null -> simulation mode)
            var triggerNode = new HWKUltra.Flow.Nodes.LightSource.Real.LightSetTriggerModeNode(null);
            triggerNode.Id = "sim-trigger";

            var context = new FlowContext();
            context.SetVariable($"{triggerNode.Id}:ChannelName", "TopLight");
            context.SetVariable($"{triggerNode.Id}:Intensity", "500");

            var result = triggerNode.ExecuteAsync(context).Result;
            if (!result.Success)
                throw new Exception($"Simulation trigger mode failed: {result.ErrorMessage}");

            var continuousNode = new HWKUltra.Flow.Nodes.LightSource.Real.LightSetContinuousModeNode(null);
            continuousNode.Id = "sim-continuous";

            var context2 = new FlowContext();
            context2.SetVariable($"{continuousNode.Id}:ChannelName", "BottomLight");
            context2.SetVariable($"{continuousNode.Id}:Intensity", "1");

            var result2 = continuousNode.ExecuteAsync(context2).Result;
            if (!result2.Success)
                throw new Exception($"Simulation continuous mode failed: {result2.ErrorMessage}");

            var onoffNode = new HWKUltra.Flow.Nodes.LightSource.Real.LightTurnOnOffNode(null);
            onoffNode.Id = "sim-onoff";

            var context3 = new FlowContext();
            context3.SetVariable($"{onoffNode.Id}:ChannelName", "SideLight");
            context3.SetVariable($"{onoffNode.Id}:Value", "true");

            var result3 = onoffNode.ExecuteAsync(context3).Result;
            if (!result3.Success)
                throw new Exception($"Simulation on/off failed: {result3.ErrorMessage}");

            Console.WriteLine("OK LightSource nodes simulation mode validated");
        }

        /// <summary>
        /// Run all LightSource builder tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========== LightSourceBuilder Tests Start ==========");
            Test_CcsLightSource_Deserialization();
            Test_LightChannelConfig_Fields();
            Test_GenericLightSourceBuilder();
            Test_CcsLightSourceBuilder();
            Test_LightSourceRouter_NameAccess();
            Test_LightSourceRouter_TriggerMode();
            Test_LightSourceRouter_ContinuousMode();
            Test_LightSourceRouter_InvalidChannel();
            Test_FromCcsLightSourceJsonFile();
            // Flow Node tests
            Test_LightSetTriggerModeNode_WithRouter();
            Test_LightSetContinuousModeNode_WithRouter();
            Test_LightTurnOnOffNode_WithRouter();
            Test_LightTurnOnOffNode_InvalidChannel();
            Test_NodeFactory_CreatesLightSourceNodes();
            Test_LightSourceNodes_Simulation();
            Console.WriteLine("========== LightSourceBuilder Tests Complete ==========");
        }

        /// <summary>
        /// Inline test JSON (matches ConfigJson/LightSource/CcsLightSource.json structure)
        /// </summary>
        private static string GetTestCcsLightSourceJson()
        {
            return @"{
                ""IpAddress"": ""192.168.1.80"",
                ""Port"": 40001,
                ""ConnectionTimeoutMs"": 3000,
                ""CommandDelayMs"": 100,
                ""Channels"": [
                    {
                        ""Name"": ""TopLight"",
                        ""ChannelIndex"": 0,
                        ""DefaultIntensity"": 512,
                        ""MaxIntensity"": 1023
                    },
                    {
                        ""Name"": ""BottomLight"",
                        ""ChannelIndex"": 1,
                        ""DefaultIntensity"": 256,
                        ""MaxIntensity"": 1023
                    },
                    {
                        ""Name"": ""SideLight"",
                        ""ChannelIndex"": 2,
                        ""DefaultIntensity"": 400,
                        ""MaxIntensity"": 1023
                    },
                    {
                        ""Name"": ""BackLight"",
                        ""ChannelIndex"": 3,
                        ""DefaultIntensity"": 300,
                        ""MaxIntensity"": 1023
                    }
                ]
            }";
        }
    }

    /// <summary>
    /// Mock light source controller for unit testing (no real hardware required).
    /// Tracks all operations for verification.
    /// </summary>
    internal class MockLightSourceController : ILightSourceController
    {
        private readonly Dictionary<int, int> _intensities = new();
        private readonly Dictionary<int, bool> _onStates = new();
        private readonly Dictionary<int, LightPulseMode> _pulseModes = new();

        public void Open() { }
        public void Close() { }

        public void TurnOn(int channel)
        {
            _onStates[channel] = true;
        }

        public void TurnOff(int channel)
        {
            _onStates[channel] = false;
        }

        public void SetIntensity(int channel, int intensity)
        {
            _intensities[channel] = intensity;
        }

        public void SetPulseMode(int channel, LightPulseMode mode)
        {
            _pulseModes[channel] = mode;
        }

        public int GetIntensity(int channel)
        {
            return _intensities.TryGetValue(channel, out var v) ? v : 0;
        }

        public bool IsOn(int channel)
        {
            return _onStates.TryGetValue(channel, out var v) && v;
        }

        public LightPulseMode GetPulseMode(int channel)
        {
            return _pulseModes.TryGetValue(channel, out var v) ? v : LightPulseMode.Off;
        }
    }
}
