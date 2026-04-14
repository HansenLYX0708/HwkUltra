using HWKUltra.UnitTest;

namespace HWKUltra.UnitTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========== HWKUltra Complete Test Suite ==========\n");

            //// Phase 1: MotionBuilder Tests (JSON config)
            //Console.WriteLine("[Phase 1] MotionBuilder Tests");
            //MotionBuilderTest.RunAllTests();

            //// Phase 2: MotionRouter Tests (JSON config)
            //Console.WriteLine("\n[Phase 2] MotionRouter Tests");
            //MotionRouterTest.RunAllTests();

            //// Phase 3: IOBuilder Tests (JSON config)
            //Console.WriteLine("\n[Phase 3] IOBuilder Tests");
            //IOBuilderTest.RunAllTests();

            // Phase 3.5: LightSourceBuilder Tests (JSON config)
            Console.WriteLine("\n[Phase 3.5] LightSourceBuilder Tests");
            LightSourceBuilderTest.RunAllTests();

            // Phase 3.6: CameraBuilder Tests (JSON config)
            Console.WriteLine("\n[Phase 3.6] CameraBuilder Tests");
            await CameraBuilderTest.RunAllTests();

            // Phase 3.7: AutoFocusBuilder Tests (JSON config)
            Console.WriteLine("\n[Phase 3.7] AutoFocusBuilder Tests");
            await AutoFocusBuilderTest.RunAllTests();

            // Phase 3.8: MeasurementBuilder Tests (JSON config)
            Console.WriteLine("\n[Phase 3.8] MeasurementBuilder Tests");
            await MeasurementBuilderTest.RunAllTests();

            //// Phase 4: Flow Engine Tests
            //Console.WriteLine("\n[Phase 4] Flow Engine Tests");
            //FlowTest.RunAllTests();

            //// Phase 5: Async Flow Execution
            //Console.WriteLine("\n[Phase 5] Async Flow Execution");
            //await FlowTest.Test4_ExecuteFlowAsync();

            //// Phase 6: Multi-Flow Concurrent Test (TestFlows.json)
            //Console.WriteLine("\n[Phase 6] Multi-Flow Concurrent Test");
            //await FlowTest.Test5_MultiFlowAsync();

            Console.WriteLine("\n========== All Tests Complete ==========");
        }
    }
}
