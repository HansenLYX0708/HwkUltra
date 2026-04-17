using System.Text.Json;
using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Services;

namespace HWKUltra.UnitTest
{
    public class TeachDataTest
    {
        private static string GetTestJsonPath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "HWKUltra_TeachData_Test");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "TeachData.json");
        }

        private static string CreateTestJson()
        {
            var json = """
            {
              "groups": [
                { "name": "BarcodeScanner", "description": "扫码枪位置", "requiredAxes": ["X", "Y", "Z"] },
                { "name": "Wait", "description": "等待位置", "requiredAxes": ["X", "Y", "Z"] }
              ],
              "positions": [
                { "name": "BarcodeScannerPos", "group": "BarcodeScanner", "description": "扫码枪工作位", "axes": {"X": 100.0, "Y": 200.0, "Z": 50.0} },
                { "name": "WaitPos", "group": "Wait", "description": "安全等待位", "axes": {"X": 0.0, "Y": 0.0, "Z": 100.0} },
                { "name": "WaitPosZ", "group": "Wait", "description": "Z轴安全高度", "axes": {"Z": 100.0} }
              ]
            }
            """;

            var path = GetTestJsonPath();
            File.WriteAllText(path, json);
            return path;
        }

        private static void Cleanup()
        {
            var dir = Path.Combine(Path.GetTempPath(), "HWKUltra_TeachData_Test");
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        /// <summary>
        /// Test 1: JSON deserialization of TeachDataConfig
        /// </summary>
        public static void Test_JsonDeserialization()
        {
            Console.WriteLine("----- TeachData JSON Deserialization -----");
            var json = File.ReadAllText(CreateTestJson());
            var config = JsonSerializer.Deserialize(json, TeachDataJsonContext.Default.TeachDataConfig);

            if (config == null) throw new Exception("Deserialization returned null");
            if (config.Groups.Count != 2) throw new Exception($"Groups count: expected 2, got {config.Groups.Count}");
            if (config.Positions.Count != 3) throw new Exception($"Positions count: expected 3, got {config.Positions.Count}");
            if (config.Groups[0].Name != "BarcodeScanner") throw new Exception($"Group name mismatch: {config.Groups[0].Name}");
            if (config.Positions[0].Axes["X"] != 100.0) throw new Exception($"X value mismatch: {config.Positions[0].Axes["X"]}");

            Console.WriteLine("OK TeachDataConfig deserialization validated");
        }

        /// <summary>
        /// Test 2: Service load and query
        /// </summary>
        public static void Test_ServiceLoadAndQuery()
        {
            Console.WriteLine("----- TeachData Service Load & Query -----");
            var path = CreateTestJson();
            var svc = new TeachDataService();
            svc.Load(path);

            if (!svc.IsLoaded) throw new Exception("Service not loaded");
            if (svc.GetPositionNames().Count != 3) throw new Exception($"Position count: {svc.GetPositionNames().Count}");
            if (svc.GetGroupNames().Count != 2) throw new Exception($"Group count: {svc.GetGroupNames().Count}");

            var pos = svc.GetPosition("BarcodeScannerPos") ?? throw new Exception("Position not found");
            if (pos.Group != "BarcodeScanner") throw new Exception($"Group mismatch: {pos.Group}");
            if (pos.Axes["X"] != 100.0) throw new Exception($"X mismatch: {pos.Axes["X"]}");

            var axisPos = svc.GetAxisPosition("BarcodeScannerPos");
            if (axisPos["X"] != 100.0 || axisPos["Y"] != 200.0 || axisPos["Z"] != 50.0)
                throw new Exception("AxisPosition values mismatch");

            var waitPositions = svc.GetGroup("Wait");
            if (waitPositions.Count != 2) throw new Exception($"Wait group count: {waitPositions.Count}");

            if (!svc.TryGetAxisPosition("WaitPos", out var wp)) throw new Exception("TryGet failed for WaitPos");
            if (wp["Z"] != 100.0) throw new Exception($"WaitPos Z mismatch: {wp["Z"]}");

            if (svc.TryGetAxisPosition("NonExistent", out _)) throw new Exception("TryGet should return false for NonExistent");

            Console.WriteLine("OK Service load and query validated");
        }

        /// <summary>
        /// Test 3: Set and remove positions
        /// </summary>
        public static void Test_ServiceSetAndRemove()
        {
            Console.WriteLine("----- TeachData Service Set & Remove -----");
            var path = CreateTestJson();
            var svc = new TeachDataService();
            svc.Load(path);

            svc.SetPosition("NewPos", "BarcodeScanner", Pos.XYZ(1.0, 2.0, 3.0), "A new position");
            if (svc.GetPositionNames().Count != 4) throw new Exception("Count should be 4 after add");

            var newPos = svc.GetPosition("NewPos") ?? throw new Exception("NewPos not found");
            if (newPos.Axes["X"] != 1.0) throw new Exception("NewPos X mismatch");

            svc.SetPosition("BarcodeScannerPos", "BarcodeScanner", Pos.XYZ(999.0, 888.0, 777.0));
            if (svc.GetAxisPosition("BarcodeScannerPos")["X"] != 999.0) throw new Exception("Update failed");

            if (!svc.UpdateAxes("WaitPos", Pos.XYZ(10.0, 20.0, 30.0))) throw new Exception("UpdateAxes failed");
            if (svc.GetAxisPosition("WaitPos")["X"] != 10.0) throw new Exception("UpdateAxes value mismatch");
            if (svc.UpdateAxes("NonExistent", Pos.XYZ(0, 0, 0))) throw new Exception("UpdateAxes should fail for NonExistent");

            if (!svc.RemovePosition("WaitPosZ")) throw new Exception("RemovePosition failed");
            if (svc.GetPositionNames().Count != 3) throw new Exception("Count should be 3 after remove");
            if (svc.GetPosition("WaitPosZ") != null) throw new Exception("WaitPosZ should be null after remove");

            Console.WriteLine("OK Set and remove validated");
        }

        /// <summary>
        /// Test 4: Save and reload
        /// </summary>
        public static void Test_SaveAndReload()
        {
            Console.WriteLine("----- TeachData Save & Reload -----");
            var path = CreateTestJson();
            var svc = new TeachDataService();
            svc.Load(path);

            svc.SetPosition("SaveTestPos", "Wait", Pos.XYZ(42.0, 43.0, 44.0), "For save test");
            svc.Save();

            var svc2 = new TeachDataService();
            svc2.Load(path);

            var pos = svc2.GetPosition("SaveTestPos") ?? throw new Exception("SaveTestPos not found after reload");
            if (pos.Axes["X"] != 42.0) throw new Exception($"X mismatch after reload: {pos.Axes["X"]}");
            if (pos.Description != "For save test") throw new Exception("Description mismatch after reload");

            Console.WriteLine("OK Save and reload validated");
        }

        /// <summary>
        /// Test 5: Auto-create group
        /// </summary>
        public static void Test_AutoCreateGroup()
        {
            Console.WriteLine("----- TeachData Auto Create Group -----");
            var svc = new TeachDataService();
            svc.LoadFrom(new TeachDataConfig());

            svc.SetPosition("Pos1", "AutoGroup", Pos.XYZ(1, 2, 3));
            if (svc.GetGroupNames().Count != 1) throw new Exception("Group count should be 1");
            if (svc.GetGroupNames()[0] != "AutoGroup") throw new Exception("Group name mismatch");

            Console.WriteLine("OK Auto-create group validated");
        }

        /// <summary>
        /// Test 6: Group management
        /// </summary>
        public static void Test_GroupManagement()
        {
            Console.WriteLine("----- TeachData Group Management -----");
            var path = CreateTestJson();
            var svc = new TeachDataService();
            svc.Load(path);

            svc.SetGroup("NewGroup", "A new group", new[] { "X", "Y" });
            if (svc.GetGroupNames().Count != 3) throw new Exception("Group count should be 3");

            var info = svc.GetGroupInfo("NewGroup") ?? throw new Exception("NewGroup not found");
            if (info.Description != "A new group") throw new Exception("Description mismatch");
            if (info.RequiredAxes.Length != 2) throw new Exception("RequiredAxes length mismatch");

            svc.SetGroup("BarcodeScanner", "Updated desc");
            var updated = svc.GetGroupInfo("BarcodeScanner") ?? throw new Exception("BarcodeScanner group not found");
            if (updated.Description != "Updated desc") throw new Exception("Updated description mismatch");

            Console.WriteLine("OK Group management validated");
        }

        /// <summary>
        /// Test 7: Flow node simulation
        /// </summary>
        public static async Task Test_FlowNodeSimulation()
        {
            Console.WriteLine("----- TeachData Flow Node Simulation -----");
            var factory = new DefaultNodeFactory();

            // Helper: build engine, register sim nodes, execute
            async Task<FlowResult> RunSingleNode(FlowDefinition def)
            {
                var engine = new FlowEngine(def);
                foreach (var n in def.Nodes)
                {
                    // Use explicit simulation mode
                    var node = factory.CreateNode(n.Type, n.Properties, useSimulation: true);
                    node.Id = n.Id;
                    engine.RegisterNode(node);
                }
                return await engine.ExecuteAsync();
            }

            // Test MoveToTeachPosition
            {
                var flowDef = new FlowDefinition();
                flowDef.Nodes.Add(new NodeDefinition
                {
                    Id = "move1",
                    Type = "MoveToTeachPosition",
                    Properties = new Dictionary<string, string>
                    {
                        ["PositionName"] = "TestPos",
                        ["GroupName"] = "XYZ"
                    }
                });
                flowDef.StartNodeId = "move1";

                var result = await RunSingleNode(flowDef);
                if (!result.Success) throw new Exception($"MoveToTeachPosition failed: {result.ErrorMessage}");
            }

            // Test GetTeachPosition
            {
                var flowDef = new FlowDefinition();
                flowDef.Nodes.Add(new NodeDefinition
                {
                    Id = "get1",
                    Type = "GetTeachPosition",
                    Properties = new Dictionary<string, string>
                    {
                        ["PositionName"] = "TestPos"
                    }
                });
                flowDef.StartNodeId = "get1";

                var result = await RunSingleNode(flowDef);
                if (!result.Success) throw new Exception($"GetTeachPosition failed: {result.ErrorMessage}");
            }

            // Test SetTeachPosition
            {
                var flowDef = new FlowDefinition();
                flowDef.Nodes.Add(new NodeDefinition
                {
                    Id = "set1",
                    Type = "SetTeachPosition",
                    Properties = new Dictionary<string, string>
                    {
                        ["PositionName"] = "TestPos",
                        ["Group"] = "Test",
                        ["X"] = "1.0",
                        ["Y"] = "2.0",
                        ["Z"] = "3.0"
                    }
                });
                flowDef.StartNodeId = "set1";

                var result = await RunSingleNode(flowDef);
                if (!result.Success) throw new Exception($"SetTeachPosition failed: {result.ErrorMessage}");
            }

            Console.WriteLine("OK Flow node simulation validated (3 node types)");
        }

        public static void RunAllTests()
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("  TeachData Tests");
            Console.WriteLine("=======================================");

            int pass = 0, fail = 0;

            var syncTests = new (string Name, Action Test)[]
            {
                ("Test 1: JSON Deserialization", Test_JsonDeserialization),
                ("Test 2: Service Load & Query", Test_ServiceLoadAndQuery),
                ("Test 3: Set & Remove", Test_ServiceSetAndRemove),
                ("Test 4: Save & Reload", Test_SaveAndReload),
                ("Test 5: Auto Create Group", Test_AutoCreateGroup),
                ("Test 6: Group Management", Test_GroupManagement),
            };

            foreach (var (testName, test) in syncTests)
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

            // Async test
            try
            {
                Test_FlowNodeSimulation().GetAwaiter().GetResult();
                pass++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL Test 7: Flow Node Simulation: {ex.Message}");
                fail++;
            }

            Cleanup();

            Console.WriteLine("=======================================");
            Console.WriteLine($"  TeachData Tests Summary: {pass} pass, {fail} fail");
            Console.WriteLine("=======================================");

            if (fail > 0)
                throw new Exception($"TeachData tests failed: {fail} test(s) failed");
        }
    }
}
