namespace HWKUltra.TestRun.Abstractions
{
    /// <summary>
    /// Handle to a single active test run.
    /// Thread-safe: use <see cref="Mutate"/> for any change to the report;
    /// subscribers to <see cref="Updated"/> receive the change after the lock is released.
    /// </summary>
    public interface ITestRun
    {
        /// <summary>Key identifying this run (e.g., tray instance name).</summary>
        string RunKey { get; }

        /// <summary>Current status.</summary>
        TestRunStatus Status { get; }

        /// <summary>
        /// Snapshot reference to the underlying report.
        /// Callers must NOT mutate outside <see cref="Mutate"/>.
        /// </summary>
        ITestRunReport Report { get; }

        /// <summary>
        /// Raised whenever the report is mutated or the status changes.
        /// </summary>
        event EventHandler<TestRunEventArgs>? Updated;

        /// <summary>
        /// Thread-safe mutation entry point. The action is invoked under an internal lock
        /// with the concrete report instance; the <see cref="Updated"/> event is raised
        /// AFTER the lock is released (subscribers may be on a background thread).
        /// </summary>
        /// <typeparam name="TReport">Concrete report type.</typeparam>
        /// <param name="mutator">Callback that mutates the report in-place.</param>
        void Mutate<TReport>(Action<TReport> mutator) where TReport : class, ITestRunReport;

        /// <summary>
        /// Move the run to a terminal state and raise the Completed event.
        /// </summary>
        /// <param name="finalStatus">Must be Completed, Failed, or Cancelled.</param>
        void Complete(TestRunStatus finalStatus);
    }
}
