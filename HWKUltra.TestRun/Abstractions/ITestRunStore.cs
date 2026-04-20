namespace HWKUltra.TestRun.Abstractions
{
    /// <summary>
    /// Cross-cutting in-memory registry of active test runs.
    /// Singleton-scoped; written to by Flow orchestration nodes,
    /// read / subscribed by UI (progress) and Communication (MES upload).
    /// </summary>
    public interface ITestRunStore
    {
        /// <summary>
        /// Register a new run under the given key with an initial report.
        /// The run starts in <see cref="TestRunStatus.Running"/> state.
        /// Throws if a run with this key is already active.
        /// </summary>
        ITestRun Start(string runKey, ITestRunReport initialReport);

        /// <summary>Get an active run by key, or null if none exists.</summary>
        ITestRun? Get(string runKey);

        /// <summary>
        /// Remove a completed run from the store (frees memory after consumers have processed it).
        /// No-op if the key is not found.
        /// </summary>
        void Remove(string runKey);

        /// <summary>Snapshot of currently-active runs.</summary>
        IReadOnlyCollection<ITestRun> ActiveRuns { get; }

        /// <summary>Raised when a new run is registered.</summary>
        event EventHandler<TestRunEventArgs>? RunStarted;

        /// <summary>Raised when any registered run's report is mutated.</summary>
        event EventHandler<TestRunEventArgs>? RunUpdated;

        /// <summary>Raised when a run transitions to a terminal state.</summary>
        event EventHandler<TestRunEventArgs>? RunCompleted;
    }
}
