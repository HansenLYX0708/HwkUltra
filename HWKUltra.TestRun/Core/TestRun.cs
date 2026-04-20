using HWKUltra.TestRun.Abstractions;

namespace HWKUltra.TestRun.Core
{
    /// <summary>
    /// Single active run. Thread-safe report mutation via an internal lock;
    /// events fire after the lock is released so subscribers can't deadlock.
    /// </summary>
    public class TestRun : ITestRun
    {
        private readonly object _lock = new();
        private readonly ITestRunReport _report;
        private TestRunStatus _status;

        public string RunKey { get; }
        public TestRunStatus Status
        {
            get { lock (_lock) return _status; }
        }
        public ITestRunReport Report => _report;

        public event EventHandler<TestRunEventArgs>? Updated;

        public TestRun(string runKey, ITestRunReport report, TestRunStatus initialStatus = TestRunStatus.Running)
        {
            if (string.IsNullOrWhiteSpace(runKey))
                throw new ArgumentException("runKey must be non-empty", nameof(runKey));
            RunKey = runKey;
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _status = initialStatus;
        }

        public void Mutate<TReport>(Action<TReport> mutator) where TReport : class, ITestRunReport
        {
            if (mutator is null) throw new ArgumentNullException(nameof(mutator));

            TestRunStatus statusSnapshot;
            lock (_lock)
            {
                if (_status is TestRunStatus.Completed or TestRunStatus.Failed or TestRunStatus.Cancelled)
                    throw new InvalidOperationException($"Cannot mutate run '{RunKey}' in terminal state {_status}.");

                if (_report is not TReport typed)
                    throw new InvalidOperationException(
                        $"Report type mismatch: expected {typeof(TReport).Name}, actual {_report.GetType().Name}.");

                mutator(typed);
                statusSnapshot = _status;
            }

            Updated?.Invoke(this, new TestRunEventArgs(
                TestRunEventKind.ReportUpdated, RunKey, statusSnapshot, _report));
        }

        public void Complete(TestRunStatus finalStatus)
        {
            if (finalStatus is not (TestRunStatus.Completed or TestRunStatus.Failed or TestRunStatus.Cancelled))
                throw new ArgumentException(
                    $"finalStatus must be Completed, Failed, or Cancelled (got {finalStatus}).",
                    nameof(finalStatus));

            lock (_lock)
            {
                if (_status == finalStatus) return; // idempotent
                if (_status is TestRunStatus.Completed or TestRunStatus.Failed or TestRunStatus.Cancelled)
                    throw new InvalidOperationException(
                        $"Cannot transition run '{RunKey}' from terminal {_status} to {finalStatus}.");
                _status = finalStatus;
            }

            Updated?.Invoke(this, new TestRunEventArgs(
                TestRunEventKind.Completed, RunKey, finalStatus, _report));
        }
    }
}
