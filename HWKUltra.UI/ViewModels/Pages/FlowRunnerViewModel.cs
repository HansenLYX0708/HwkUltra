using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Services;
using HWKUltra.Flow.Utils;
using HWKUltra.UI.Services;
using Microsoft.Win32;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.ViewModels.Pages
{
    public partial class FlowRunnerViewModel : ObservableObject, INavigationAware
    {
        private readonly NodeCatalogService _catalogService;
        private DefaultNodeFactory? _nodeFactory;
        private FlowEngine? _engine;
        private FlowDefinition? _definition;
        private CancellationTokenSource? _cts;
        private Stopwatch? _stopwatch;
        private SharedFlowContext? _sharedContext;

        public FlowRunnerViewModel(NodeCatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        public Task OnNavigatedToAsync()
        {
            EnsureFactory();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        #region Observable Properties

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResumeCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        private FlowRunState _runState = FlowRunState.Idle;

        [ObservableProperty]
        private string _flowName = "(No flow loaded)";

        [ObservableProperty]
        private string _flowDescription = "";

        [ObservableProperty]
        private string _flowFilePath = "";

        [ObservableProperty]
        private int _totalNodes;

        [ObservableProperty]
        private int _executedNodes;

        [ObservableProperty]
        private string _currentNodeName = "";

        [ObservableProperty]
        private string _elapsedTime = "00:00.000";

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private bool _useSimulation = true;

        public ObservableCollection<FlowLogEntry> LogEntries { get; } = new();

        // Lightweight post-run summary (count + CSV path, if any). No per-row grid during
        // execution so UI polling never impacts flow timing.
        [ObservableProperty]
        private int _flowResultsCount;

        [ObservableProperty]
        private string _flowResultsCsvPath = "";

        [ObservableProperty]
        private bool _hasCsvPath;

        [ObservableProperty]
        private bool _hasFlowResults;

        [ObservableProperty]
        private string _poolSummary = "";

        [ObservableProperty]
        private bool _hasPoolSummary;

        #endregion

        #region Commands

        [RelayCommand]
        private void ImportFlow()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import Flow JSON",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Path.Combine(AppContext.BaseDirectory, "ConfigJson", "Flow")
            };

            if (dlg.ShowDialog() != true) return;
            LoadFlowFile(dlg.FileName);
        }

        [RelayCommand]
        private void OpenRecent()
        {
            // Browse the ConfigJson/Flow folder
            var flowDir = Path.Combine(AppContext.BaseDirectory, "ConfigJson", "Flow");
            if (!Directory.Exists(flowDir)) return;

            var dlg = new OpenFileDialog
            {
                Title = "Select Flow JSON",
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = flowDir
            };

            if (dlg.ShowDialog() == true)
                LoadFlowFile(dlg.FileName);
        }

        private bool CanExecute => RunState == FlowRunState.Idle && _definition != null;

        [RelayCommand(CanExecute = nameof(CanExecute))]
        private async Task Execute()
        {
            if (_definition == null) return;

            EnsureFactory();
            if (_nodeFactory == null) { AddLog("ERROR", "Node factory not available"); return; }

            // Clean up any leftover state from previous runs to prevent memory leaks.
            // This ensures that if Execute is called multiple times, old references
            // to engine, context, cancellation token, etc. are properly released.
            if (_engine != null)
            {
                _engine.NodeExecuting -= OnNodeExecuting;
                _engine.NodeExecuted -= OnNodeExecuted;
                _engine.FlowError -= OnFlowError;
                _engine = null;
            }
            _sharedContext = null;
            _cts?.Dispose();
            _cts = null;
            _stopwatch = null;

            // Clear UI-bound state
            RunState = FlowRunState.Running;
            ExecutedNodes = 0;
            CurrentNodeName = "";
            LogEntries.Clear();
            FlowResultsCount = 0;
            FlowResultsCsvPath = "";
            HasFlowResults = false;
            PoolSummary = "";
            HasPoolSummary = false;

            AddLog("INFO", $"Starting flow: {_definition.Name} ({TotalNodes} nodes)");

            _cts = new CancellationTokenSource();
            _stopwatch = Stopwatch.StartNew();

            // Create engine
            var executionDef = CloneDefinition(_definition);
            _engine = new FlowEngine(executionDef);

            foreach (var nodeDef in executionDef.Nodes)
            {
                try
                {
                    var node = _nodeFactory.CreateNode(nodeDef.Type, nodeDef.Properties);
                    node.Id = nodeDef.Id;
                    node.Name = nodeDef.Name;
                    node.Description = nodeDef.Description;
                    _engine.RegisterNode(node);
                }
                catch (Exception ex)
                {
                    AddLog("ERROR", $"Failed to create node '{nodeDef.Type}': {ex.Message}");
                    RunState = FlowRunState.Idle;
                    return;
                }
            }

            _engine.NodeExecuting += OnNodeExecuting;
            _engine.NodeExecuted += OnNodeExecuted;
            _engine.FlowError += OnFlowError;
            // Use named handlers so we can unsubscribe on completion (prevents the
            // engine + its dispatcher-queued closures from being held by the VM
            // beyond the run, which retains FlowContext / SharedContext in Gen2).
            EventHandler pausedHandler = (_, _) => Dispatch(() => AddLog("INFO", "Flow paused"));
            EventHandler resumedHandler = (_, _) => Dispatch(() => AddLog("INFO", "Flow resumed"));
            _engine.FlowPaused += pausedHandler;
            _engine.FlowResumed += resumedHandler;

            // Build context
            _sharedContext = new SharedFlowContext();
            var context = new FlowContext { NodeFactory = _nodeFactory, SharedContext = _sharedContext };
            // Set CurrentFlowDirectory so SubFlowNode/ParallelNode can resolve relative paths
            if (!string.IsNullOrEmpty(executionDef.SourceFilePath))
                context.CurrentFlowDirectory = Path.GetDirectoryName(executionDef.SourceFilePath);
            // Propagate sub-flow/parallel node execution logs to the UI log panel
            context.OnNodeLog = (flowName, nodeName, nodeType, isStart, result) =>
            {
                if (isStart)
                    Dispatch(() => AddLog("SUB", $"  [{flowName}] ▶ {nodeName} ({nodeType})"));
                else
                {
                    var status = result?.Success == true ? "OK" : $"FAIL: {result?.ErrorMessage}";
                    Dispatch(() => AddLog("SUB", $"  [{flowName}] ✓ {nodeName} [{status}]"));
                }
            };
            foreach (var nodeDef in executionDef.Nodes)
                foreach (var prop in nodeDef.Properties)
                    context.Variables[$"{nodeDef.Id}:{prop.Key}"] = prop.Value;

            // Timer update only (no result grid polling — avoids interfering with flow timing).
            _ = Task.Run(async () =>
            {
                while (RunState is FlowRunState.Running or FlowRunState.Paused)
                {
                    Dispatch(() => ElapsedTime = _stopwatch?.Elapsed.ToString(@"mm\:ss\.fff") ?? "");
                    await Task.Delay(250);
                }
            });

            try
            {
                var result = await Task.Run(() => _engine.ExecuteAsync(context, _cts.Token));
                _stopwatch.Stop();
                ElapsedTime = _stopwatch.Elapsed.ToString(@"mm\:ss\.fff");

                if (result.Success)
                {
                    AddLog("SUCCESS", $"Flow completed in {_stopwatch.Elapsed.TotalSeconds:F2}s");
                    StatusText = "Completed";
                }
                else
                {
                    AddLog("ERROR", $"Flow failed: {result.ErrorMessage}");
                    StatusText = $"Failed: {result.ErrorMessage}";
                }
            }
            catch (OperationCanceledException)
            {
                _stopwatch.Stop();
                ElapsedTime = _stopwatch.Elapsed.ToString(@"mm\:ss\.fff");
                AddLog("WARN", "Flow cancelled by user");
                StatusText = "Cancelled";
            }
            catch (Exception ex)
            {
                _stopwatch?.Stop();
                AddLog("ERROR", $"Unexpected error: {ex.Message}");
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                // Final refresh so any last-minute rows are displayed.
                // Must run BEFORE we drop _sharedContext (it reads from it).
                RefreshFlowResults();
                RefreshActivePools();

                // Unsubscribe all engine events so the engine + its delegate chain
                // (which transitively holds FlowContext via dispatcher-queued closures)
                // can be GC'd promptly after the run.
                if (_engine != null)
                {
                    _engine.NodeExecuting -= OnNodeExecuting;
                    _engine.NodeExecuted -= OnNodeExecuted;
                    _engine.FlowError -= OnFlowError;
                    _engine.FlowPaused -= pausedHandler;
                    _engine.FlowResumed -= resumedHandler;
                }

                // Sever the OnNodeLog closure (captures `this`) so the FlowContext
                // and any nested child contexts can be released.
                context.OnNodeLog = null;

                // Clear all temporary variables to enable prompt GC.
                // These local variables hold references to large objects (FlowDefinition,
                // FlowContext, event handlers). Setting them to null allows the GC
                // to reclaim memory immediately after the run completes.
                executionDef = null;
                context = null;
                pausedHandler = null;
                resumedHandler = null;

                RunState = FlowRunState.Idle;
                _engine = null;
                // Release SharedContext so its accumulated variables (LeftPocketJobs,
                // FlowResults lists, signals, etc.) become eligible for GC. Without
                // this, the previous run's data lingers in Gen2 until the next run
                // overwrites the field.
                _sharedContext = null;
                _stopwatch = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private bool CanPause => RunState == FlowRunState.Running;

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            _engine?.PauseWithSharedContext(_sharedContext);
            RunState = FlowRunState.Paused;
            StatusText = "Paused";
        }

        private bool CanResume => RunState == FlowRunState.Paused;

        [RelayCommand(CanExecute = nameof(CanResume))]
        private void Resume()
        {
            _engine?.ResumeWithSharedContext(_sharedContext);
            RunState = FlowRunState.Running;
            StatusText = "Running";
        }

        private bool CanStop => RunState is FlowRunState.Running or FlowRunState.Paused;

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            _engine?.ResumeWithSharedContext(_sharedContext); // unblock if paused so cancellation can propagate
            _cts?.Cancel();
            StatusText = "Stopping...";
        }

        [RelayCommand]
        private void ClearLog()
        {
            LogEntries.Clear();
        }

        [RelayCommand]
        private void ExportLog()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export Log",
                Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
                FileName = $"FlowLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dlg.ShowDialog() != true) return;

            var lines = LogEntries.Select(e => $"{e.Timestamp:HH:mm:ss.fff}\t[{e.Level}]\t{e.Message}");
            File.WriteAllLines(dlg.FileName, lines);
            AddLog("INFO", $"Log exported to {dlg.FileName}");
        }

        #endregion

        #region Engine Event Handlers

        private void OnNodeExecuting(object? sender, FlowNodeEventArgs e)
        {
            // Extract primitive values BEFORE Dispatch so the queued lambda does not
            // capture the FlowNodeEventArgs (which transitively pins FlowContext +
            // its Variables dictionary). This was a major Gen2 retention source under
            // high-frequency loops (e.g. Gen5 100-cycle scans).
            var nodeName = e.Node.Name;
            var nodeType = e.Node.NodeType;
            Dispatch(() =>
            {
                CurrentNodeName = nodeName;
                AddLog("NODE", $"▶ Executing: {nodeName} ({nodeType})");
            });
        }

        private void OnNodeExecuted(object? sender, FlowNodeEventArgs e)
        {
            // Extract values up front (see OnNodeExecuting comment).
            var nodeName = e.Node.Name;
            var success = e.Result?.Success == true;
            var errorMsg = e.Result?.ErrorMessage;
            var branchLabel = e.Result?.BranchLabel;
            Dispatch(() =>
            {
                ExecutedNodes++;
                var status = success ? "OK" : $"FAIL: {errorMsg}";
                var branch = !string.IsNullOrEmpty(branchLabel) ? $" → {branchLabel}" : "";
                AddLog("NODE", $"✓ Completed: {nodeName}{branch} [{status}]");
            });
        }

        /// <summary>
        /// Post-run summary: how many result rows were collected, and where the CSV
        /// (if any) was saved. Called once after flow completion — never during the run.
        /// </summary>
        private void RefreshFlowResults()
        {
            if (_sharedContext == null) return;
            var list = _sharedContext.GetVariable<List<Dictionary<string, object>>>("FlowResults");
            if (list == null) { HasFlowResults = false; return; }

            int count;
            lock (list) count = list.Count;

            FlowResultsCount = count;
            FlowResultsCsvPath = _sharedContext.GetVariable<string>("FlowResults_CsvPath") ?? "";
            HasCsvPath = !string.IsNullOrEmpty(FlowResultsCsvPath);
            HasFlowResults = count > 0;
        }

        /// <summary>
        /// Post-run pool summary: totals for the main ImagePool (if any existed).
        /// </summary>
        private void RefreshActivePools()
        {
            if (_sharedContext == null) return;
            var probed = new[] { "ImagePool", "FrameQueue", "CameraPool", "Pool" };
            foreach (var name in probed)
            {
                var pool = _sharedContext.GetVariable<ImagePool>(name);
                if (pool == null) continue;
                PoolSummary = $"{pool.Name}: produced={pool.TotalEnqueued}, processed={pool.TotalDequeued}"
                    + (pool.TotalDropped > 0 ? $", dropped={pool.TotalDropped}" : "");
                HasPoolSummary = true;
                return;
            }
            HasPoolSummary = false;
        }

        [RelayCommand]
        private void OpenCsvFolder()
        {
            if (string.IsNullOrEmpty(FlowResultsCsvPath) || !File.Exists(FlowResultsCsvPath)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{FlowResultsCsvPath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { AddLog("WARN", $"Open folder failed: {ex.Message}"); }
        }

        private void OnFlowError(object? sender, FlowErrorEventArgs e)
        {
            // Extract values up front so the queued lambda does not capture the
            // FlowErrorEventArgs / FlowContext.
            var nodeName = e.Node?.Name ?? "?";
            var errorMsg = e.ErrorMessage;
            Dispatch(() =>
            {
                AddLog("ERROR", $"✗ Error at {nodeName}: {errorMsg}");
            });
        }

        #endregion

        #region Private Helpers

        private void EnsureFactory()
        {
            if (_nodeFactory != null) return;
            try
            {
                // All config paths left null → all devices fall back to simulation
                var builder = new NodeFactoryBuilder();
                _nodeFactory = builder.Build();
            }
            catch (Exception ex)
            {
                AddLog("WARN", $"Factory init: {ex.Message}");
            }
        }

        private void LoadFlowFile(string path)
        {
            try
            {
                var def = FlowSerializer.LoadFromFile(path);
                if (def == null) { AddLog("ERROR", $"Failed to parse: {path}"); return; }

                _definition = def;
                FlowFilePath = path;
                FlowName = def.Name;
                FlowDescription = def.Description ?? "";
                TotalNodes = def.Nodes.Count;
                ExecutedNodes = 0;
                StatusText = "Flow loaded";

                ExecuteCommand.NotifyCanExecuteChanged();
                AddLog("INFO", $"Loaded: {def.Name} — {def.Nodes.Count} nodes, {def.Connections.Count} connections");
            }
            catch (Exception ex)
            {
                AddLog("ERROR", $"Import failed: {ex.Message}");
            }
        }

        private static FlowDefinition CloneDefinition(FlowDefinition src)
        {
            return new FlowDefinition
            {
                Id = src.Id,
                Name = src.Name,
                Description = src.Description,
                CreatedAt = src.CreatedAt,
                ModifiedAt = src.ModifiedAt,
                Version = src.Version,
                StartNodeId = src.StartNodeId,
                Nodes = src.Nodes.ToList(),
                Connections = src.Connections.ToList(),
                GlobalVariables = src.GlobalVariables.ToList(),
                SourceFilePath = src.SourceFilePath
            };
        }

        private void AddLog(string level, string message)
        {
            var entry = new FlowLogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };
            LogEntries.Add(entry);
            // Cap log size to prevent unbounded UI memory growth during long runs
            // (each per-node Executing/Executed event adds an entry; a multi-cycle
            // flow with parallel workers can otherwise accumulate hundreds of
            // thousands of entries).
            const int MaxLogEntries = 5000;
            while (LogEntries.Count > MaxLogEntries)
                LogEntries.RemoveAt(0);
        }

        private static void Dispatch(Action action)
        {
            var app = Application.Current;
            if (app is null) { action(); return; }
            if (app.Dispatcher.CheckAccess()) action();
            else app.Dispatcher.BeginInvoke(action);
        }

        #endregion
    }

    public enum FlowRunState
    {
        Idle,
        Running,
        Paused
    }

    public class FlowLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
    }

}
