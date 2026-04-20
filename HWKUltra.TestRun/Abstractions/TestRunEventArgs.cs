namespace HWKUltra.TestRun.Abstractions
{
    /// <summary>
    /// Kind of test-run lifecycle event.
    /// </summary>
    public enum TestRunEventKind
    {
        /// <summary>A new run was registered in the store.</summary>
        Started = 0,

        /// <summary>An active run's report was mutated.</summary>
        ReportUpdated = 1,

        /// <summary>The run completed (Completed/Failed/Cancelled).</summary>
        Completed = 2
    }

    /// <summary>
    /// Payload for test-run lifecycle and update events.
    /// </summary>
    public class TestRunEventArgs : EventArgs
    {
        /// <summary>Kind of event.</summary>
        public TestRunEventKind Kind { get; }

        /// <summary>Run key (e.g., tray instance name) identifying the run.</summary>
        public string RunKey { get; }

        /// <summary>Run status at the moment the event was raised.</summary>
        public TestRunStatus Status { get; }

        /// <summary>Live report snapshot (reference to the underlying object; caller must not mutate outside <c>Mutate</c>).</summary>
        public ITestRunReport Report { get; }

        /// <summary>UTC timestamp when the event was raised.</summary>
        public DateTime Timestamp { get; }

        public TestRunEventArgs(TestRunEventKind kind, string runKey, TestRunStatus status, ITestRunReport report)
        {
            Kind = kind;
            RunKey = runKey;
            Status = status;
            Report = report;
            Timestamp = DateTime.UtcNow;
        }
    }
}
