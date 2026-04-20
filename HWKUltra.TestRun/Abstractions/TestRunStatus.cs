namespace HWKUltra.TestRun.Abstractions
{
    /// <summary>
    /// Lifecycle state of a test run.
    /// </summary>
    public enum TestRunStatus
    {
        /// <summary>Run has been created but not started.</summary>
        Idle = 0,

        /// <summary>Run is actively accumulating data.</summary>
        Running = 1,

        /// <summary>Run finished normally.</summary>
        Completed = 2,

        /// <summary>Run finished with errors.</summary>
        Failed = 3,

        /// <summary>Run was cancelled before completion.</summary>
        Cancelled = 4
    }
}
