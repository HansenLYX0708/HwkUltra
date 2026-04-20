namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Standalone entry for running only LLM tests. Invoke via:
    ///   dotnet run --project HWKUltra.UnitTest -- llm
    /// </summary>
    public static class LLMTestRunner
    {
        public static async Task<int> RunIfRequested(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("llm", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("========== LLM Service Tests Only ==========\n");
                await LLMServiceTest.RunAllTests();
                return 1;
            }
            return 0;
        }
    }
}
