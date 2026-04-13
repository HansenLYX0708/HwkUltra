using HWKUltra.UnitTest;

namespace HWKUltra.UnitTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========== HWKUltra Complete Test Suite ==========\n");

            // Phase 1: MotionBuilder Tests (JSON config)
            Console.WriteLine("[Phase 1] MotionBuilder Tests");
            MotionBuilderTest.RunAllTests();

            // Phase 2: MotionRouter Tests (JSON config)
            Console.WriteLine("\n[Phase 2] MotionRouter Tests");
            MotionRouterTest.RunAllTests();

            // Phase 3: Flow Engine Tests
            Console.WriteLine("\n[Phase 3] Flow Engine Tests");
            FlowTest.RunAllTests();

            // Phase 4: Async Flow Execution
            Console.WriteLine("\n[Phase 4] Async Flow Execution");
            await FlowTest.Test4_ExecuteFlowAsync();

            // Phase 5: Multi-Flow Concurrent Test (TestFlows.json)
            Console.WriteLine("\n[Phase 5] Multi-Flow Concurrent Test");
            await FlowTest.Test5_MultiFlowAsync();

            Console.WriteLine("\n========== All Tests Complete ==========");
        }
    }
}
