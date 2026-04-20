using System.Collections.Concurrent;
using HWKUltra.TestRun.Abstractions;

namespace HWKUltra.TestRun.Core
{
    /// <summary>
    /// Thread-safe registry of active runs. Intended as a process-wide singleton.
    /// Aggregates per-run Updated events into a single RunUpdated event stream
    /// so UI / Communication can subscribe once.
    /// </summary>
    public class TestRunStore : ITestRunStore
    {
        private readonly ConcurrentDictionary<string, TestRun> _runs = new();

        public event EventHandler<TestRunEventArgs>? RunStarted;
        public event EventHandler<TestRunEventArgs>? RunUpdated;
        public event EventHandler<TestRunEventArgs>? RunCompleted;

        public ITestRun Start(string runKey, ITestRunReport initialReport)
        {
            if (string.IsNullOrWhiteSpace(runKey))
                throw new ArgumentException("runKey must be non-empty", nameof(runKey));
            if (initialReport is null) throw new ArgumentNullException(nameof(initialReport));

            var run = new TestRun(runKey, initialReport, TestRunStatus.Running);

            if (!_runs.TryAdd(runKey, run))
                throw new InvalidOperationException($"A run with key '{runKey}' is already active.");

            run.Updated += OnRunUpdated;

            RunStarted?.Invoke(this, new TestRunEventArgs(
                TestRunEventKind.Started, runKey, TestRunStatus.Running, initialReport));

            return run;
        }

        public ITestRun? Get(string runKey)
            => _runs.TryGetValue(runKey, out var run) ? run : null;

        public void Remove(string runKey)
        {
            if (_runs.TryRemove(runKey, out var run))
            {
                run.Updated -= OnRunUpdated;
            }
        }

        public IReadOnlyCollection<ITestRun> ActiveRuns => _runs.Values.Cast<ITestRun>().ToList();

        private void OnRunUpdated(object? sender, TestRunEventArgs e)
        {
            switch (e.Kind)
            {
                case TestRunEventKind.ReportUpdated:
                    RunUpdated?.Invoke(this, e);
                    break;
                case TestRunEventKind.Completed:
                    RunCompleted?.Invoke(this, e);
                    break;
            }
        }
    }
}
