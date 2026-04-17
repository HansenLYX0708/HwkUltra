// Communication builder tests - validates config, builder, router, and Flow nodes
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.Communication;
using HWKUltra.Communication.Abstractions;
using HWKUltra.Communication.Core;
using HWKUltra.Communication.Implementations;
using HWKUltra.Communication.Implementations.WDConnect;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Communication.Real;
using HWKUltra.Flow.Services;

namespace HWKUltra.UnitTest
{
    public class CommunicationBuilderTest
    {
        /// <summary>
        /// Test 1: JSON deserialization of WDConnectCommunicationControllerConfig
        /// </summary>
        public static void Test_Communication_Deserialization()
        {
            Console.WriteLine("----- Communication JSON Deserialization -----");

            var json = GetTestJson();
            var config = JsonSerializer.Deserialize(json, CommunicationJsonContext.Default.WDConnectCommunicationControllerConfig);

            if (config == null)
                throw new Exception("WDConnectCommunicationControllerConfig deserialization failed: returned null");

            Console.WriteLine("OK Communication config deserialization validated");
            Console.WriteLine($"  - ToolModelPath: {config.ToolModelPath}");
            Console.WriteLine($"  - AutoConnect: {config.AutoConnect}");
        }

        /// <summary>
        /// Test 2: Config field validation
        /// </summary>
        public static void Test_Communication_ConfigFields()
        {
            Console.WriteLine("----- Communication Config Fields -----");

            var json = GetTestJson();
            var config = JsonSerializer.Deserialize(json, CommunicationJsonContext.Default.WDConnectCommunicationControllerConfig)!;

            if (config.ToolModelPath != "Equipment/ToolModel.xml")
                throw new Exception($"ToolModelPath mismatch: expected Equipment/ToolModel.xml, got {config.ToolModelPath}");
            if (config.AutoConnect != false)
                throw new Exception($"AutoConnect mismatch: expected false, got {config.AutoConnect}");

            Console.WriteLine("OK Communication config fields validated");
        }

        /// <summary>
        /// Test 3: Generic CommunicationBuilder
        /// </summary>
        public static void Test_Generic_CommunicationBuilder()
        {
            Console.WriteLine("----- Generic CommunicationBuilder -----");

            var builder = new CommunicationBuilder<WDConnectCommunicationControllerConfig>(
                config => new WDConnectCommunicationController(config));

            builder.WithJsonDeserializer(json =>
            {
                var config = JsonSerializer.Deserialize(json, CommunicationJsonContext.Default.WDConnectCommunicationControllerConfig);
                return config ?? throw new Exception("Deserialization returned null");
            });

            var json2 = GetTestJson();
            builder.FromJson(json2);

            // Just test that builder creates a controller without exception
            var controller = builder.BuildController();
            if (controller == null)
                throw new Exception("Generic builder returned null controller");

            Console.WriteLine("OK Generic CommunicationBuilder validated");
        }

        /// <summary>
        /// Test 4: Dedicated WDConnectCommunicationBuilder
        /// </summary>
        public static void Test_Dedicated_CommunicationBuilder()
        {
            Console.WriteLine("----- Dedicated WDConnectCommunicationBuilder -----");

            var builder = new WDConnectCommunicationBuilder();
            builder.FromJson(GetTestJson());

            var controller = builder.BuildController();
            if (controller == null)
                throw new Exception("Dedicated builder returned null controller");

            var router = new WDConnectCommunicationBuilder().FromJson(GetTestJson()).BuildRouter();
            if (router == null)
                throw new Exception("Dedicated builder returned null router");

            Console.WriteLine("OK Dedicated WDConnectCommunicationBuilder validated");
        }

        /// <summary>
        /// Test 5: CommunicationCompleteData DTO creation
        /// </summary>
        public static void Test_CommunicationCompleteData()
        {
            Console.WriteLine("----- CommunicationCompleteData DTO -----");

            var data = new CommunicationCompleteData
            {
                TrayId = "TRAY-001",
                LoadLock = "L",
                EmpId = "OP-001",
                DefectSliders = new List<SliderDefectInfo>
                {
                    new SliderDefectInfo { ContainerId = "C1", SliderSN = "SN-1", Row = 1, Col = 18, DefectCode = "A2" },
                    new SliderDefectInfo { ContainerId = "C2", SliderSN = "SN-2", Row = 3, Col = 5, DefectCode = "P0532" }
                }
            };

            if (data.TrayId != "TRAY-001") throw new Exception("TrayId mismatch");
            if (data.DefectSliders.Count != 2) throw new Exception($"DefectSliders count: expected 2, got {data.DefectSliders.Count}");
            if (data.DefectSliders[0].DefectCode != "A2") throw new Exception($"DefectCode[0]: expected A2, got {data.DefectSliders[0].DefectCode}");
            if (data.DefectSliders[1].DefectCode != "P0532") throw new Exception($"DefectCode[1]: expected P0532, got {data.DefectSliders[1].DefectCode}");

            Console.WriteLine("OK CommunicationCompleteData DTO validated");
            Console.WriteLine($"  - DefectSliders: {data.DefectSliders.Count}");
        }

        /// <summary>
        /// Test 6: JSON file load via builder
        /// </summary>
        public static void Test_CommunicationJsonFile()
        {
            Console.WriteLine("----- Communication JSON File -----");

            var tempDir = Path.Combine(Path.GetTempPath(), "HWKUltra_CommTest");
            var tempFile = Path.Combine(tempDir, "TestComm.json");
            try
            {
                Directory.CreateDirectory(tempDir);
                File.WriteAllText(tempFile, GetTestJson());

                var builder = new WDConnectCommunicationBuilder();
                builder.FromJsonFile(tempFile);
                var controller = builder.BuildController();
                if (controller == null)
                    throw new Exception("JSON file load failed");

                Console.WriteLine("OK Communication JSON file load validated");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Test 7: Flow node simulation for all 8 Communication nodes
        /// </summary>
        public static async Task Test_CommunicationFlowNodeSimulation()
        {
            Console.WriteLine("----- Communication Flow Node Simulation -----");

            var factory = new DefaultNodeFactory();
            factory.UseSimulation = true;
            var context = new FlowContext { CancellationToken = CancellationToken.None };

            // CommunicationOpen
            var openNode = factory.CreateNode("CommunicationOpen", new Dictionary<string, string>());
            var result = await openNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationOpen failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationOpen simulation passed");

            // CommunicationClose
            var closeNode = factory.CreateNode("CommunicationClose", new Dictionary<string, string>());
            result = await closeNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationClose failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationClose simulation passed");

            // CommunicationStartScan
            var scanNode = factory.CreateNode("CommunicationStartScan", new Dictionary<string, string>());
            context.Variables[$"{scanNode.Id}:TrayId"] = "TEST-TRAY";
            context.Variables[$"{scanNode.Id}:LoadLock"] = "L";
            context.Variables[$"{scanNode.Id}:EmpId"] = "OP001";
            result = await scanNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationStartScan failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationStartScan simulation passed");

            // CommunicationLoad
            var loadNode = factory.CreateNode("CommunicationLoad", new Dictionary<string, string>());
            context.Variables[$"{loadNode.Id}:LoadLock"] = "L";
            context.Variables[$"{loadNode.Id}:EmpId"] = "OP001";
            result = await loadNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationLoad failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationLoad simulation passed");

            // CommunicationUnload
            var unloadNode = factory.CreateNode("CommunicationUnload", new Dictionary<string, string>());
            context.Variables[$"{unloadNode.Id}:LoadLock"] = "R";
            context.Variables[$"{unloadNode.Id}:EmpId"] = "OP001";
            result = await unloadNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationUnload failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationUnload simulation passed");

            // CommunicationComplete
            var completeNode = factory.CreateNode("CommunicationComplete", new Dictionary<string, string>());
            context.Variables[$"{completeNode.Id}:TrayId"] = "TEST-TRAY";
            context.Variables[$"{completeNode.Id}:LoadLock"] = "L";
            context.Variables[$"{completeNode.Id}:EmpId"] = "OP001";
            result = await completeNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationComplete failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationComplete simulation passed");

            // CommunicationAbort
            var abortNode = factory.CreateNode("CommunicationAbort", new Dictionary<string, string>());
            context.Variables[$"{abortNode.Id}:TrayId"] = "TEST-TRAY";
            context.Variables[$"{abortNode.Id}:LoadLock"] = "L";
            context.Variables[$"{abortNode.Id}:EmpId"] = "OP001";
            result = await abortNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationAbort failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationAbort simulation passed");

            // CommunicationLogin
            var loginNode = factory.CreateNode("CommunicationLogin", new Dictionary<string, string>());
            context.Variables[$"{loginNode.Id}:UserId"] = "admin";
            context.Variables[$"{loginNode.Id}:Password"] = "pass123";
            result = await loginNode.ExecuteAsync(context);
            if (!result.Success) throw new Exception($"CommunicationLogin failed: {result.ErrorMessage}");
            Console.WriteLine("  OK CommunicationLogin simulation passed");

            Console.WriteLine("OK All Communication Flow node simulations passed");
        }

        /// <summary>
        /// Run all Communication tests
        /// </summary>
        public static async Task RunAllTests()
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("  Communication Builder Tests");
            Console.WriteLine("=======================================");

            int pass = 0, fail = 0;

            void Run(string name, Action test)
            {
                try { test(); pass++; }
                catch (Exception ex) { Console.WriteLine($"FAIL {name}: {ex.Message}"); fail++; }
            }

            async Task RunAsync(string name, Func<Task> test)
            {
                try { await test(); pass++; }
                catch (Exception ex) { Console.WriteLine($"FAIL {name}: {ex.Message}"); fail++; }
            }

            Run("Test 1: JSON Deserialization", Test_Communication_Deserialization);
            Run("Test 2: Config Fields", Test_Communication_ConfigFields);
            Run("Test 3: Generic Builder", Test_Generic_CommunicationBuilder);
            Run("Test 4: Dedicated Builder", Test_Dedicated_CommunicationBuilder);
            Run("Test 5: CompleteData DTO", Test_CommunicationCompleteData);
            Run("Test 6: JSON File", Test_CommunicationJsonFile);
            await RunAsync("Test 7: Flow Node Simulation", Test_CommunicationFlowNodeSimulation);

            Console.WriteLine("=======================================");
            Console.WriteLine($"  Communication Tests Summary: {pass} pass, {fail} fail");
            Console.WriteLine("=======================================");

            if (fail > 0)
                throw new Exception($"Communication tests failed: {fail} test(s) failed");
        }

        private static string GetTestJson()
        {
            return """
            {
                "ToolModelPath": "Equipment/ToolModel.xml",
                "AutoConnect": false
            }
            """;
        }
    }
}
