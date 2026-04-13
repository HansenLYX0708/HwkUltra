using HWKUltra.Builder.Tests;
using HWKUltra.UnitTest;

namespace HWKUltra.UnitTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // MotionBuilderTest.RunAllTests();
            MotionRouterTest.RunAllTests();
            FlowTest.RunAllTests();
            await FlowTest.Test4_ExecuteFlowAsync();
        }
    }
}
