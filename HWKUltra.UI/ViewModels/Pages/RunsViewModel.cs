using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.UI.ViewModels.Pages
{
    /// <summary>
    /// Live view of active / recently-completed runs from <see cref="ITestRunStore"/>.
    /// Subscribes to RunStarted / RunUpdated / RunCompleted events and marshals
    /// updates onto the UI dispatcher.
    /// </summary>
    public partial class RunsViewModel : ObservableObject, IDisposable
    {
        private readonly ITestRunStore _store;
        private bool _disposed;

        public ObservableCollection<RunRowViewModel> Rows { get; } = new();

        [ObservableProperty]
        private int _activeCount;

        [ObservableProperty]
        private int _completedCount;

        public RunsViewModel(ITestRunStore store)
        {
            _store = store;
            _store.RunStarted += OnRunStarted;
            _store.RunUpdated += OnRunUpdated;
            _store.RunCompleted += OnRunCompleted;

            // Seed with any runs already in the store
            foreach (var run in _store.ActiveRuns)
                Rows.Add(RunRowViewModel.From(run.RunKey, run.Status, run.Report));
            Recompute();
        }

        private void OnRunStarted(object? sender, TestRunEventArgs e) =>
            Dispatch(() =>
            {
                var existing = FindRow(e.RunKey);
                if (existing is null)
                    Rows.Insert(0, RunRowViewModel.From(e.RunKey, e.Status, e.Report));
                else
                    existing.UpdateFrom(e.Status, e.Report);
                Recompute();
            });

        private void OnRunUpdated(object? sender, TestRunEventArgs e) =>
            Dispatch(() =>
            {
                var row = FindRow(e.RunKey);
                row?.UpdateFrom(e.Status, e.Report);
            });

        private void OnRunCompleted(object? sender, TestRunEventArgs e) =>
            Dispatch(() =>
            {
                var row = FindRow(e.RunKey);
                row?.UpdateFrom(e.Status, e.Report);
                Recompute();
            });

        private RunRowViewModel? FindRow(string key)
        {
            foreach (var r in Rows)
                if (r.RunKey == key) return r;
            return null;
        }

        private void Recompute()
        {
            int active = 0, completed = 0;
            foreach (var r in Rows)
            {
                if (r.Status == TestRunStatus.Running) active++;
                else if (r.Status == TestRunStatus.Completed) completed++;
            }
            ActiveCount = active;
            CompletedCount = completed;
        }

        private static void Dispatch(Action action)
        {
            var app = Application.Current;
            if (app is null) { action(); return; }
            if (app.Dispatcher.CheckAccess()) action();
            else app.Dispatcher.BeginInvoke(action);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _store.RunStarted -= OnRunStarted;
            _store.RunUpdated -= OnRunUpdated;
            _store.RunCompleted -= OnRunCompleted;
            _disposed = true;
        }
    }

    /// <summary>One row in the runs grid.</summary>
    public partial class RunRowViewModel : ObservableObject
    {
        [ObservableProperty] private string _runKey = "";
        [ObservableProperty] private string _trayName = "";
        [ObservableProperty] private string _serial = "";
        [ObservableProperty] private TestRunStatus _status;
        [ObservableProperty] private int _defectCount;
        [ObservableProperty] private int _rows;
        [ObservableProperty] private int _cols;
        [ObservableProperty] private DateTime? _startTime;
        [ObservableProperty] private DateTime? _endTime;

        public string StatusText => Status.ToString();

        public static RunRowViewModel From(string key, TestRunStatus status, ITestRunReport report)
        {
            var row = new RunRowViewModel { RunKey = key };
            row.UpdateFrom(status, report);
            return row;
        }

        public void UpdateFrom(TestRunStatus status, ITestRunReport report)
        {
            Status = status;
            OnPropertyChanged(nameof(StatusText));
            if (report is TrayAoiReport tray)
            {
                TrayName = tray.TrayName;
                Serial = tray.Session.SerialNumber;
                DefectCount = tray.Defects.Count;
                Rows = tray.Rows;
                Cols = tray.Cols;
                StartTime = tray.Session.StartTime == default ? null : tray.Session.StartTime;
                EndTime = tray.Session.EndTime == default ? null : tray.Session.EndTime;
            }
        }
    }
}
