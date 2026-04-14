// Tray builder tests - validates TrayController configuration, TrayRouter, and Flow nodes
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.Tray;
using HWKUltra.Tray.Abstractions;
using HWKUltra.Tray.Core;
using HWKUltra.Tray.Implementations;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Tray.Real;

namespace HWKUltra.UnitTest
{
    public class TrayBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of TrayControllerConfig
        /// </summary>
        public static void Test_TrayController_Deserialization()
        {
            Console.WriteLine("----- Tray JSON Deserialization Validation -----");

            var json = GetTestTrayJson();
            var config = JsonSerializer.Deserialize(json, TrayJsonContext.Default.TrayControllerConfig);

            if (config == null)
                throw new Exception("TrayControllerConfig deserialization failed: returned null");

            if (config.Instances == null || config.Instances.Count != 2)
                throw new Exception($"Instances count mismatch: expected 2, got {config.Instances?.Count}");

            Console.WriteLine("OK TrayControllerConfig deserialization validated");
            Console.WriteLine($"  - Instances: {config.Instances.Count}");
        }

        /// <summary>
        /// Test 2: TrayConfig field validation
        /// </summary>
        public static void Test_TrayConfig_Fields()
        {
            Console.WriteLine("----- TrayConfig Field Validation -----");

            var json = GetTestTrayJson();
            var config = JsonSerializer.Deserialize(json, TrayJsonContext.Default.TrayControllerConfig)!;

            var tray1 = config.Instances[0];
            if (tray1.Name != "Tray1") throw new Exception($"Name mismatch: expected Tray1, got {tray1.Name}");
            if (tray1.Rows != 8) throw new Exception($"Rows mismatch: expected 8, got {tray1.Rows}");
            if (tray1.Cols != 30) throw new Exception($"Cols mismatch: expected 30, got {tray1.Cols}");

            var tray2 = config.Instances[1];
            if (tray2.Name != "Tray2") throw new Exception($"Name mismatch: expected Tray2, got {tray2.Name}");
            if (tray2.Rows != 4) throw new Exception($"Rows mismatch: expected 4, got {tray2.Rows}");
            if (tray2.Cols != 10) throw new Exception($"Cols mismatch: expected 10, got {tray2.Cols}");

            Console.WriteLine("OK TrayConfig fields validated");
        }

        /// <summary>
        /// Test 3: Generic TrayBuilder
        /// </summary>
        public static void Test_GenericTrayBuilder()
        {
            Console.WriteLine("----- Generic TrayBuilder Validation -----");

            var builder = new TrayBuilder<TrayControllerConfig>(
                config => new TrayController(config),
                config => config.Instances.ToDictionary(i => i.Name, i => (object)i));
            builder.WithJsonDeserializer(json =>
                JsonSerializer.Deserialize(json, TrayJsonContext.Default.TrayControllerConfig)!);

            var router = builder.FromJson(GetTestTrayJson()).BuildRouter();

            if (!router.HasInstance("Tray1"))
                throw new Exception("Generic builder: Tray1 instance not found");
            if (!router.HasInstance("Tray2"))
                throw new Exception("Generic builder: Tray2 instance not found");

            Console.WriteLine("OK Generic TrayBuilder validated");
        }

        /// <summary>
        /// Test 4: Dedicated TrayBuilder
        /// </summary>
        public static void Test_DedicatedTrayBuilder()
        {
            Console.WriteLine("----- Dedicated TrayBuilder Validation -----");

            var router = new TrayBuilder().FromJson(GetTestTrayJson()).BuildRouter();

            if (router.InstanceNames.Count != 2)
                throw new Exception($"Instance count mismatch: expected 2, got {router.InstanceNames.Count}");

            Console.WriteLine("OK Dedicated TrayBuilder validated");
        }

        /// <summary>
        /// Test 5: Multi-instance support & core operations
        /// </summary>
        public static void Test_MultiInstance_Operations()
        {
            Console.WriteLine("----- Multi-Instance Operations Validation -----");

            var router = new TrayBuilder().FromJson(GetTestTrayJson()).BuildRouter();

            // Verify instance names
            if (!router.HasInstance("Tray1") || !router.HasInstance("Tray2"))
                throw new Exception("Multi-instance: missing instances");

            // Test GetTrayInfo
            var info1 = router.GetTrayInfo("Tray1");
            if (info1.Rows != 8 || info1.Cols != 30 || info1.TotalSlots != 240)
                throw new Exception($"Tray1 info mismatch: {info1.Rows}x{info1.Cols}={info1.TotalSlots}");

            var info2 = router.GetTrayInfo("Tray2");
            if (info2.Rows != 4 || info2.Cols != 10 || info2.TotalSlots != 40)
                throw new Exception($"Tray2 info mismatch: {info2.Rows}x{info2.Cols}={info2.TotalSlots}");

            // Test 4-corner teach & position retrieval
            router.InitPositions("Tray2",
                new Point3D(0, 0, 0),
                new Point3D(90, 0, 0),
                new Point3D(0, 30, 0),
                new Point3D(90, 30, 0));

            var pos00 = router.GetPocketPosition("Tray2", 0, 0);
            if (Math.Abs(pos00.X) > 0.001 || Math.Abs(pos00.Y) > 0.001)
                throw new Exception($"Position (0,0) mismatch: {pos00}");

            var pos39 = router.GetPocketPosition("Tray2", 3, 9);
            if (Math.Abs(pos39.X - 90) > 0.001 || Math.Abs(pos39.Y - 30) > 0.001)
                throw new Exception($"Position (3,9) mismatch: {pos39}");

            // Test slot state
            router.SetSlotState("Tray2", 1, 1, SlotState.Pass);
            var state = router.GetSlotState("Tray2", 1, 1);
            if (state != SlotState.Pass)
                throw new Exception($"SlotState mismatch: expected Pass, got {state}");

            // Test reset
            router.ResetTray("Tray2");
            state = router.GetSlotState("Tray2", 1, 1);
            if (state != SlotState.Empty)
                throw new Exception($"SlotState after reset: expected Empty, got {state}");

            // Test test state
            router.SetTestState("Tray2", TrayTestState.Testing);
            var testState = router.GetTestState("Tray2");
            if (testState != TrayTestState.Testing)
                throw new Exception($"TestState mismatch: expected Testing, got {testState}");

            Console.WriteLine("OK Multi-instance operations validated");
        }

        /// <summary>
        /// Test 6: JSON file loading
        /// </summary>
        public static void Test_JsonFile_Loading()
        {
            Console.WriteLine("----- JSON File Loading Validation -----");

            string jsonPath = Path.Combine("ConfigJson", "Tray", "TrayConfig.json");
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"SKIP JSON file not found: {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize(json, TrayJsonContext.Default.TrayControllerConfig);
            if (config == null || config.Instances == null || config.Instances.Count == 0)
                throw new Exception("Failed to load TrayControllerConfig from file");

            Console.WriteLine($"OK JSON file loaded: {config.Instances.Count} instances");
        }

        /// <summary>
        /// Test 7: Flow node simulation
        /// </summary>
        public static void Test_FlowNode_Simulation()
        {
            Console.WriteLine("----- Tray Flow Node Simulation -----");

            var context = new FlowContext();

            // TrayInitNode
            var initNode = new TrayInitNode(null);
            context.Variables[$"{initNode.Id}:InstanceName"] = "Tray1";
            context.Variables[$"{initNode.Id}:Rows"] = "8";
            context.Variables[$"{initNode.Id}:Cols"] = "30";
            var result = initNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"TrayInitNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK TrayInitNode simulation passed");

            // TrayTeachNode
            var teachNode = new TrayTeachNode(null);
            context.Variables[$"{teachNode.Id}:InstanceName"] = "Tray1";
            context.Variables[$"{teachNode.Id}:LT_X"] = "0";
            context.Variables[$"{teachNode.Id}:LT_Y"] = "0";
            context.Variables[$"{teachNode.Id}:LT_Z"] = "0";
            context.Variables[$"{teachNode.Id}:RT_X"] = "100";
            context.Variables[$"{teachNode.Id}:RT_Y"] = "0";
            context.Variables[$"{teachNode.Id}:RT_Z"] = "0";
            context.Variables[$"{teachNode.Id}:LB_X"] = "0";
            context.Variables[$"{teachNode.Id}:LB_Y"] = "100";
            context.Variables[$"{teachNode.Id}:LB_Z"] = "0";
            context.Variables[$"{teachNode.Id}:RB_X"] = "100";
            context.Variables[$"{teachNode.Id}:RB_Y"] = "100";
            context.Variables[$"{teachNode.Id}:RB_Z"] = "0";
            result = teachNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"TrayTeachNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK TrayTeachNode simulation passed");

            // TrayGetPositionNode
            var getPosNode = new TrayGetPositionNode(null);
            context.Variables[$"{getPosNode.Id}:InstanceName"] = "Tray1";
            context.Variables[$"{getPosNode.Id}:Row"] = "0";
            context.Variables[$"{getPosNode.Id}:Col"] = "0";
            result = getPosNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"TrayGetPositionNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK TrayGetPositionNode simulation passed");

            // TraySetSlotStateNode
            var setStateNode = new TraySetSlotStateNode(null);
            context.Variables[$"{setStateNode.Id}:InstanceName"] = "Tray1";
            context.Variables[$"{setStateNode.Id}:Row"] = "0";
            context.Variables[$"{setStateNode.Id}:Col"] = "0";
            context.Variables[$"{setStateNode.Id}:State"] = "2";
            result = setStateNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"TraySetSlotStateNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK TraySetSlotStateNode simulation passed");

            // TrayGetSlotStateNode
            var getStateNode = new TrayGetSlotStateNode(null);
            context.Variables[$"{getStateNode.Id}:InstanceName"] = "Tray1";
            context.Variables[$"{getStateNode.Id}:Row"] = "0";
            context.Variables[$"{getStateNode.Id}:Col"] = "0";
            result = getStateNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"TrayGetSlotStateNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK TrayGetSlotStateNode simulation passed");

            // TrayResetNode
            var resetNode = new TrayResetNode(null);
            context.Variables[$"{resetNode.Id}:InstanceName"] = "Tray1";
            result = resetNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"TrayResetNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK TrayResetNode simulation passed");

            // TrayGetInfoNode
            var infoNode = new TrayGetInfoNode(null);
            context.Variables[$"{infoNode.Id}:InstanceName"] = "Tray1";
            result = infoNode.ExecuteAsync(context).GetAwaiter().GetResult();
            if (!result.Success) throw new Exception($"TrayGetInfoNode failed: {result.ErrorMessage}");
            Console.WriteLine("  OK TrayGetInfoNode simulation passed");

            Console.WriteLine("OK All Tray Flow node simulations passed");
        }

        public static void RunAllTests()
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("  Tray Builder Tests");
            Console.WriteLine("=======================================");
            int pass = 0, fail = 0;

            var tests = new (string Name, Action Test)[]
            {
                ("Test 1: JSON Deserialization", Test_TrayController_Deserialization),
                ("Test 2: Config Fields", Test_TrayConfig_Fields),
                ("Test 3: Generic Builder", Test_GenericTrayBuilder),
                ("Test 4: Dedicated Builder", Test_DedicatedTrayBuilder),
                ("Test 5: Multi-Instance Ops", Test_MultiInstance_Operations),
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
            Console.WriteLine($"  Tray Tests Summary: {pass} pass, {fail} fail");
            Console.WriteLine("=======================================");

            if (fail > 0)
                throw new Exception($"Tray tests failed: {fail} test(s) failed");
        }

        private static string GetTestTrayJson()
        {
            return """
            {
                "Instances": [
                    {
                        "Name": "Tray1",
                        "Rows": 8,
                        "Cols": 30,
                        "PositionDataPath": ""
                    },
                    {
                        "Name": "Tray2",
                        "Rows": 4,
                        "Cols": 10,
                        "PositionDataPath": ""
                    }
                ]
            }
            """;
        }
    }
}
