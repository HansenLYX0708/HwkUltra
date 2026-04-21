using HWKUltra.UnitTest;

namespace HWKUltra.UnitTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Allow running only LLM tests with: dotnet run --project HWKUltra.UnitTest -- llm
            if (await LLMTestRunner.RunIfRequested(args) > 0)
                return;

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
            //Console.WriteLine("\n[Phase 3.5] LightSourceBuilder Tests");
            //LightSourceBuilderTest.RunAllTests();

            //// Phase 3.6: CameraBuilder Tests (JSON config)
            //Console.WriteLine("\n[Phase 3.6] CameraBuilder Tests");
            //await CameraBuilderTest.RunAllTests();

            //// Phase 3.7: AutoFocusBuilder Tests (JSON config)
            //Console.WriteLine("\n[Phase 3.7] AutoFocusBuilder Tests");
            //await AutoFocusBuilderTest.RunAllTests();

            //// Phase 3.8: MeasurementBuilder Tests (JSON config)
            //Console.WriteLine("\n[Phase 3.8] MeasurementBuilder Tests");
            //await MeasurementBuilderTest.RunAllTests();

            //// Phase 3.9: TrayBuilder Tests (JSON config)
            //Console.WriteLine("\n[Phase 3.9] TrayBuilder Tests");
            //TrayBuilderTest.RunAllTests();

            //// Phase 4.0: BarcodeScannerBuilder Tests (JSON config)
            //Console.WriteLine("\n[Phase 4.0] BarcodeScannerBuilder Tests");
            //BarcodeScannerBuilderTest.RunAllTests();

            //// Phase 4.1: CommunicationBuilder Tests (JSON config)
            //Console.WriteLine("\n[Phase 4.1] CommunicationBuilder Tests");
            //await CommunicationBuilderTest.RunAllTests();

            //// Phase 4: Flow Engine Tests
            //Console.WriteLine("\n[Phase 4] Flow Engine Tests");
            //FlowTest.RunAllTests();

            //// Phase 5: Async Flow Execution
            //Console.WriteLine("\n[Phase 5] Async Flow Execution");
            //await FlowTest.Test4_ExecuteFlowAsync();

            //// Phase 6: Multi-Flow Concurrent Test (TestFlows.json)
            //Console.WriteLine("\n[Phase 6] Multi-Flow Concurrent Test");
            //await FlowTest.Test5_MultiFlowAsync();

            // Phase 7: Flow Parallel Extension Tests
            //Console.WriteLine("\n[Phase 7] Flow Parallel Extension Tests");
            //await FlowParallelTest.RunAllTests();

            //// Phase 8: Flow Integration Tests (full AOI inspection with all new nodes)
            //Console.WriteLine("\n[Phase 8] Flow Integration Tests");
            //await FlowIntegrationTest.RunAllTests();

            //// Phase 9: TeachData Tests (models, service, flow nodes)
            //Console.WriteLine("\n[Phase 9] TeachData Tests");
            //TeachDataTest.RunAllTests();

            //// Phase 10: LLM Service Tests (AI layer)
            //Console.WriteLine("\n[Phase 10] LLM Service Tests");
            //await LLMServiceTest.RunAllTests();

            //// Phase 11: TestRun Store Tests (runtime session layer)
            //Console.WriteLine("\n[Phase 11] TestRun Store Tests");
            //TestRunStoreTest.RunAllTests();

            // Phase 12: Vision Algorithm Tests
            Console.WriteLine("\n[Phase 12] Vision Algorithm Tests");
            VisionAlgorithmTest.RunAllTests();

            // Phase 13: TestRun ↔ Communication bridge tests
            Console.WriteLine("\n[Phase 13] TestRunCompletionBridge Tests");
            TestRunCompletionBridgeTest.RunAllTests();

            Console.WriteLine("\n========== All Tests Complete ==========");
        }
    }
}
