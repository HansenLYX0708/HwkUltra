using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using Wpf.Ui.Abstractions.Controls;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Services;
using HWKUltra.Flow.Utils;
using HWKUltra.UI.Models;
using HWKUltra.UI.Services;

namespace HWKUltra.UI.ViewModels.Pages
{
    /// <summary>
    /// ViewModel for the Node Test page — test individual flow nodes with optional device environment
    /// </summary>
    public partial class NodeTestViewModel : ObservableObject, INavigationAware
    {
        private readonly NodeCatalogService _catalogService;
        private DefaultNodeFactory? _nodeFactory;
        private NodeFactoryBuilder? _factoryBuilder;
        private CancellationTokenSource? _executionCts;
        private bool _isInitialized;

        public NodeTestViewModel(NodeCatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            LoadCategories();
            _isInitialized = true;
        }

        #region Device Environment

        [ObservableProperty]
        private bool _useSimulation = true;

        [ObservableProperty]
        private string _motionConfigPath = string.Empty;

        [ObservableProperty]
        private string _ioConfigPath = string.Empty;

        [ObservableProperty]
        private string _lightSourceConfigPath = string.Empty;

        [ObservableProperty]
        private string _cameraConfigPath = string.Empty;

        [ObservableProperty]
        private string _autoFocusConfigPath = string.Empty;

        [ObservableProperty]
        private string _measurementConfigPath = string.Empty;

        [ObservableProperty]
        private string _trayConfigPath = string.Empty;

        [ObservableProperty]
        private string _barcodeScannerConfigPath = string.Empty;

        [ObservableProperty]
        private string _environmentStatus = "Not initialized";

        [ObservableProperty]
        private bool _isEnvironmentReady;

        [ObservableProperty]
        private bool _isDevicesConnected;

        public ObservableCollection<string> EnvironmentLog { get; } = new();

        /// <summary>
        /// Config JSON base directory under the exe output folder
        /// </summary>
        private static string ConfigJsonBaseDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigJson");

        /// <summary>
        /// Default config file mapping: device category -> (subfolder, filename)
        /// </summary>
        private static readonly Dictionary<string, (string Folder, string FileName)> DefaultConfigMap = new()
        {
            ["Motion"]         = ("Motion",         "ElmoMotion.json"),
            ["IO"]             = ("IO",             "GalilIO.json"),
            ["LightSource"]    = ("LightSource",    "CcsLightSource.json"),
            ["Camera"]         = ("Camera",         "BaslerCamera.json"),
            ["AutoFocus"]      = ("AutoFocus",      "LafAutoFocus.json"),
            ["Measurement"]    = ("Measurement",    "KeyenceMeasurement.json"),
            ["Tray"]           = ("Tray",           "TrayConfig.json"),
            ["BarcodeScanner"] = ("BarcodeScanner", "SerialBarcodeScanner.json"),
        };

        [RelayCommand]
        private void BrowseConfig(string parameter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = $"Select {parameter} Config File"
            };

            if (dialog.ShowDialog() == true)
            {
                switch (parameter)
                {
                    case "Motion": MotionConfigPath = dialog.FileName; break;
                    case "IO": IoConfigPath = dialog.FileName; break;
                    case "LightSource": LightSourceConfigPath = dialog.FileName; break;
                    case "Camera": CameraConfigPath = dialog.FileName; break;
                    case "AutoFocus": AutoFocusConfigPath = dialog.FileName; break;
                    case "Measurement": MeasurementConfigPath = dialog.FileName; break;
                    case "Tray": TrayConfigPath = dialog.FileName; break;
                    case "BarcodeScanner": BarcodeScannerConfigPath = dialog.FileName; break;
                }
            }
        }

        [RelayCommand]
        private void LoadDefaultConfigs()
        {
            EnvironmentLog.Clear();
            int found = 0;

            foreach (var kvp in DefaultConfigMap)
            {
                var category = kvp.Key;
                var (folder, fileName) = kvp.Value;
                var filePath = Path.Combine(ConfigJsonBaseDir, folder, fileName);

                if (File.Exists(filePath))
                {
                    SetConfigPath(category, filePath);
                    EnvironmentLog.Add($"[{category}] Loaded: {fileName}");
                    found++;
                }
                else
                {
                    EnvironmentLog.Add($"[{category}] NOT FOUND: {filePath}");
                }
            }

            // Validate JSON structure for each loaded config
            ValidateLoadedConfigs();

            EnvironmentStatus = $"Loaded {found}/{DefaultConfigMap.Count} config files";
        }

        private void SetConfigPath(string category, string path)
        {
            switch (category)
            {
                case "Motion": MotionConfigPath = path; break;
                case "IO": IoConfigPath = path; break;
                case "LightSource": LightSourceConfigPath = path; break;
                case "Camera": CameraConfigPath = path; break;
                case "AutoFocus": AutoFocusConfigPath = path; break;
                case "Measurement": MeasurementConfigPath = path; break;
                case "Tray": TrayConfigPath = path; break;
                case "BarcodeScanner": BarcodeScannerConfigPath = path; break;
            }
        }

        private void ValidateLoadedConfigs()
        {
            var paths = new Dictionary<string, string>
            {
                ["Motion"] = MotionConfigPath,
                ["IO"] = IoConfigPath,
                ["LightSource"] = LightSourceConfigPath,
                ["Camera"] = CameraConfigPath,
                ["AutoFocus"] = AutoFocusConfigPath,
                ["Measurement"] = MeasurementConfigPath,
                ["Tray"] = TrayConfigPath,
                ["BarcodeScanner"] = BarcodeScannerConfigPath,
            };

            foreach (var kvp in paths)
            {
                if (string.IsNullOrEmpty(kvp.Value)) continue;
                try
                {
                    var json = File.ReadAllText(kvp.Value);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    // Basic structure check: must be a valid JSON object
                    if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                        EnvironmentLog.Add($"  [WARN] {kvp.Key}: Root is not a JSON object");
                    else
                        EnvironmentLog.Add($"  [OK] {kvp.Key}: JSON valid ({doc.RootElement.EnumerateObject().Count()} top-level keys)");
                }
                catch (Exception ex)
                {
                    EnvironmentLog.Add($"  [ERROR] {kvp.Key}: JSON parse failed: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private void InitializeEnvironment()
        {
            EnvironmentLog.Clear();

            try
            {
                if (UseSimulation)
                {
                    _factoryBuilder = new NodeFactoryBuilder();
                    _nodeFactory = _factoryBuilder.BuildSimulated();
                    foreach (var s in _factoryBuilder.BuildLog)
                        EnvironmentLog.Add(s.ToString());
                    EnvironmentStatus = "Initialized (Simulation)";
                }
                else
                {
                    _factoryBuilder = new NodeFactoryBuilder
                    {
                        MotionConfigPath = string.IsNullOrEmpty(MotionConfigPath) ? null : MotionConfigPath,
                        IOConfigPath = string.IsNullOrEmpty(IoConfigPath) ? null : IoConfigPath,
                        LightSourceConfigPath = string.IsNullOrEmpty(LightSourceConfigPath) ? null : LightSourceConfigPath,
                        CameraConfigPath = string.IsNullOrEmpty(CameraConfigPath) ? null : CameraConfigPath,
                        AutoFocusConfigPath = string.IsNullOrEmpty(AutoFocusConfigPath) ? null : AutoFocusConfigPath,
                        MeasurementConfigPath = string.IsNullOrEmpty(MeasurementConfigPath) ? null : MeasurementConfigPath,
                        TrayConfigPath = string.IsNullOrEmpty(TrayConfigPath) ? null : TrayConfigPath,
                        BarcodeScannerConfigPath = string.IsNullOrEmpty(BarcodeScannerConfigPath) ? null : BarcodeScannerConfigPath,
                    };
                    _nodeFactory = _factoryBuilder.Build();
                    foreach (var s in _factoryBuilder.BuildLog)
                        EnvironmentLog.Add(s.ToString());

                    var realCount = _factoryBuilder.BuildLog.Count(s => !s.IsSimulated);
                    EnvironmentStatus = realCount > 0
                        ? $"Initialized ({realCount} real, {_factoryBuilder.BuildLog.Count - realCount} simulated)"
                        : "Initialized (all simulated)";
                }

                IsEnvironmentReady = true;
                IsDevicesConnected = false;
            }
            catch (Exception ex)
            {
                EnvironmentLog.Add($"[ERROR] {ex.Message}");
                EnvironmentStatus = "Initialization failed";
                IsEnvironmentReady = false;
            }
        }

        [RelayCommand]
        private void ConnectDevices()
        {
            if (_factoryBuilder == null)
            {
                EnvironmentLog.Add("[ERROR] Environment not initialized");
                return;
            }

            EnvironmentLog.Add("--- Connecting devices ---");
            var log = _factoryBuilder.ConnectAll();
            foreach (var entry in log)
                EnvironmentLog.Add(entry);

            var okCount = log.Count(l => l.Contains("] OK"));
            var failCount = log.Count(l => l.Contains("] FAIL"));
            IsDevicesConnected = failCount == 0 && okCount > 0;

            EnvironmentStatus = failCount > 0
                ? $"Connected ({okCount} ok, {failCount} failed)"
                : $"Connected ({okCount} devices)";
        }

        [RelayCommand]
        private void DisconnectDevices()
        {
            if (_factoryBuilder == null) return;

            EnvironmentLog.Add("--- Disconnecting devices ---");
            var log = _factoryBuilder.DisconnectAll();
            foreach (var entry in log)
                EnvironmentLog.Add(entry);

            IsDevicesConnected = false;
            EnvironmentStatus = "Disconnected";
        }

        [RelayCommand]
        private void ResetEnvironment()
        {
            if (IsDevicesConnected)
                DisconnectDevices();

            _factoryBuilder = null;
            _nodeFactory = null;
            IsEnvironmentReady = false;
            IsDevicesConnected = false;
            EnvironmentStatus = "Not initialized";
            EnvironmentLog.Clear();
        }

        #endregion

        #region Node Selection & Configuration

        public ObservableCollection<NodeCatalogCategory> Categories { get; } = new();
        public ObservableCollection<NodeCatalogCategory> FilteredCategories { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private NodeCatalogEntry? _selectedNodeEntry;

        public ObservableCollection<NodePropertyViewModel> NodeProperties { get; } = new();

        [ObservableProperty]
        private ObservableCollection<FlowParameter> _outputDefinitions = new();

        private void LoadCategories()
        {
            Categories.Clear();
            FilteredCategories.Clear();

            foreach (var category in _catalogService.GetCategories())
            {
                Categories.Add(category);
                FilteredCategories.Add(category);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredCategories.Clear();

            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var cat in Categories)
                    FilteredCategories.Add(cat);
                return;
            }

            var search = value.Trim().ToLowerInvariant();
            foreach (var cat in Categories)
            {
                var filtered = cat.Entries
                    .Where(e => e.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || e.Type.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || e.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filtered.Count > 0)
                {
                    FilteredCategories.Add(new NodeCatalogCategory
                    {
                        Name = cat.Name,
                        Color = cat.Color,
                        Entries = filtered
                    });
                }
            }
        }

        partial void OnSelectedNodeEntryChanged(NodeCatalogEntry? value)
        {
            NodeProperties.Clear();
            OutputDefinitions.Clear();

            if (value == null) return;

            foreach (var input in value.InputDefinitions)
            {
                NodeProperties.Add(new NodePropertyViewModel
                {
                    Name = input.Name,
                    Value = input.DefaultValue?.ToString() ?? string.Empty,
                    ParameterDef = input
                });
            }

            OutputDefinitions = new ObservableCollection<FlowParameter>(value.OutputDefinitions);
            OnPropertyChanged(nameof(OutputDefinitions));
        }

        #endregion

        #region Execution

        [ObservableProperty]
        private bool _isExecuting;

        [ObservableProperty]
        private bool _saveAsJson;

        [ObservableProperty]
        private string _saveJsonPath = string.Empty;

        [ObservableProperty]
        private string _executionResult = string.Empty;

        public ObservableCollection<string> OutputLog { get; } = new();
        public ObservableCollection<OutputValueItem> OutputValues { get; } = new();

        [RelayCommand]
        private void BrowseSavePath()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Flow As",
                FileName = $"{SelectedNodeEntry?.Type ?? "NodeTest"}_Flow.json"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveJsonPath = dialog.FileName;
            }
        }

        [RelayCommand]
        private async Task ExecuteNode()
        {
            if (SelectedNodeEntry == null)
            {
                ExecutionResult = "No node selected";
                return;
            }

            if (!IsEnvironmentReady || _nodeFactory == null)
            {
                ExecutionResult = "Environment not initialized";
                return;
            }

            OutputLog.Clear();
            OutputValues.Clear();
            ExecutionResult = "Executing...";
            IsExecuting = true;
            _executionCts = new CancellationTokenSource();

            try
            {
                // Build single-node FlowDefinition
                var nodeId = "test-node-001";
                var properties = new Dictionary<string, string>();
                foreach (var prop in NodeProperties)
                    properties[prop.Name] = prop.Value;

                var definition = new FlowDefinition
                {
                    Id = "test-flow-001",
                    Name = $"Test_{SelectedNodeEntry.Type}",
                    Description = $"Single node test for {SelectedNodeEntry.DisplayName}",
                    StartNodeId = nodeId,
                    Nodes = new List<NodeDefinition>
                    {
                        new NodeDefinition
                        {
                            Id = nodeId,
                            Type = SelectedNodeEntry.Type,
                            Name = SelectedNodeEntry.DisplayName,
                            Properties = properties
                        }
                    },
                    Connections = new List<ConnectionDefinition>()
                };

                // Save as JSON if requested
                if (SaveAsJson && !string.IsNullOrEmpty(SaveJsonPath))
                {
                    try
                    {
                        FlowSerializer.SaveToFile(definition, SaveJsonPath);
                        OutputLog.Add($"[Save] Flow saved to {SaveJsonPath}");
                    }
                    catch (Exception ex)
                    {
                        OutputLog.Add($"[Save Error] {ex.Message}");
                    }
                }

                // Capture console output
                var originalOut = Console.Out;
                var writer = new StringWriter();
                Console.SetOut(writer);

                try
                {
                    // Create node and register
                    var engine = new FlowEngine(definition);
                    var node = _nodeFactory.CreateNode(SelectedNodeEntry.Type, properties);
                    node.Id = nodeId;
                    node.Name = SelectedNodeEntry.DisplayName;
                    engine.RegisterNode(node);

                    // Build context
                    var shared = new SharedFlowContext();
                    var context = new FlowContext
                    {
                        SharedContext = shared,
                        NodeFactory = _nodeFactory,
                        CancellationToken = _executionCts.Token
                    };
                    foreach (var p in properties)
                        context.Variables[$"{nodeId}:{p.Key}"] = p.Value;

                    // Execute
                    var result = await engine.ExecuteAsync(context);

                    // Collect console output
                    Console.SetOut(originalOut);
                    var consoleOutput = writer.ToString();
                    if (!string.IsNullOrEmpty(consoleOutput))
                    {
                        foreach (var line in consoleOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            OutputLog.Add(line.TrimEnd('\r'));
                    }

                    // Show result
                    if (result.Success)
                    {
                        ExecutionResult = string.IsNullOrEmpty(result.BranchLabel)
                            ? "SUCCESS"
                            : $"SUCCESS (Branch: {result.BranchLabel})";
                    }
                    else
                    {
                        ExecutionResult = $"FAILED: {result.ErrorMessage}";
                    }

                    // Collect output values from context
                    foreach (var output in SelectedNodeEntry.OutputDefinitions)
                    {
                        var key = $"{nodeId}:{output.Name}";
                        var value = context.Variables.TryGetValue(key, out var val) ? val?.ToString() ?? "" : "(not set)";
                        OutputValues.Add(new OutputValueItem { Name = output.DisplayName ?? output.Name, Value = value });
                    }
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }
            catch (OperationCanceledException)
            {
                ExecutionResult = "CANCELLED";
                OutputLog.Add("[Cancelled] Execution was cancelled by user");
            }
            catch (Exception ex)
            {
                ExecutionResult = $"ERROR: {ex.Message}";
                OutputLog.Add($"[Exception] {ex}");
            }
            finally
            {
                IsExecuting = false;
                _executionCts?.Dispose();
                _executionCts = null;
            }
        }

        [RelayCommand]
        private void CancelExecution()
        {
            _executionCts?.Cancel();
        }

        #endregion
    }

    /// <summary>
    /// Represents a node output value after execution
    /// </summary>
    public class OutputValueItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
