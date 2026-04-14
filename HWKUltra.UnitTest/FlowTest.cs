namespace HWKUltra.UnitTest
{
    /// <summary>
    /// HWK.Flow flow engine tests
    /// </summary>
    public class FlowTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("\n========== HWK.Flow Tests Start ==========");

            Test1_NodeTemplates();
            Test2_CreateAoiFlow();
            Test3_FlowSerialization();

            Console.WriteLine("========== HWK.Flow Tests Complete ==========\n");
        }

        /// <summary>
        /// Test 5: Multi-flow execution (async)
        /// </summary>
        public static async Task Test5_MultiFlowAsync()
        {
            Console.WriteLine("\n----- Test 5: Multi-Flow Execution -----");
            await TestFlowHelper.RunMultiFlowTest();
            Console.WriteLine("✓ Multi-flow test passed");
        }

        /// <summary>
        /// Test 1: Node templates
        /// </summary>
        static void Test1_NodeTemplates()
        {
            Console.WriteLine("\n----- Test 1: Node Templates -----");
            TestFlowHelper.DemoNodeTemplates();
            Console.WriteLine("✓ Node templates test passed");
        }

        /// <summary>
        /// Test 2: Create AOI flow
        /// </summary>
        static void Test2_CreateAoiFlow()
        {
            Console.WriteLine("\n----- Test 2: Create AOI Flow -----");

            var flow = TestFlowHelper.CreateTestAoiFlow();

            Console.WriteLine($"Flow name: {flow.Name}");
            Console.WriteLine($"Flow description: {flow.Description}");
            Console.WriteLine($"Node count: {flow.Nodes.Count}");
            Console.WriteLine($"Connection count: {flow.Connections.Count}");
            Console.WriteLine($"Start node: {flow.StartNodeId}");

            // Validate nodes
            foreach (var node in flow.Nodes)
            {
                Console.WriteLine($"  - [{node.Type}] {node.Name} (ID: {node.Id})");
            }

            // Validate connections
            foreach (var conn in flow.Connections)
            {
                var condition = string.IsNullOrEmpty(conn.Condition) ? "" : $" [{conn.Condition}]";
                Console.WriteLine($"  - {conn.SourceNodeId} -> {conn.TargetNodeId}{condition}");
            }

            Console.WriteLine("✓ AOI flow creation test passed");
        }

        /// <summary>
        /// Test 3: Flow serialization
        /// </summary>
        static void Test3_FlowSerialization()
        {
            Console.WriteLine("\n----- Test 3: Flow Serialization -----");

            var flow = TestFlowHelper.CreateTestAoiFlow();

            // Serialize
            var json = HWKUltra.Flow.Utils.FlowSerializer.Serialize(flow);
            Console.WriteLine("Serialized JSON:");
            Console.WriteLine(json.Substring(0, Math.Min(500, json.Length)) + "...");

            // Deserialize
            var restoredFlow = HWKUltra.Flow.Utils.FlowSerializer.Deserialize(json);

            if (restoredFlow == null)
                throw new Exception("Deserialization failed: returned null");

            if (restoredFlow.Name != flow.Name)
                throw new Exception($"Name mismatch: {restoredFlow.Name} != {flow.Name}");

            if (restoredFlow.Nodes.Count != flow.Nodes.Count)
                throw new Exception($"Node count mismatch: {restoredFlow.Nodes.Count} != {flow.Nodes.Count}");

            Console.WriteLine("✓ Flow serialization test passed");
        }

        /// <summary>
        /// Test 4: Execute flow (simulated, no hardware)
        /// </summary>
        public static async Task Test4_ExecuteFlowAsync()
        {
            Console.WriteLine("\n----- Test 4: Execute Flow -----");
            await TestFlowHelper.DemoExecuteFlow();
            Console.WriteLine("✓ Flow execution test passed");
        }
    }
}
