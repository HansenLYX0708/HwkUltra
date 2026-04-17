using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui.Abstractions.Controls;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Models;
using HWKUltra.UI.Models;
using HWKUltra.UI.Services;

namespace HWKUltra.UI.ViewModels.Pages
{
    /// <summary>
    /// ViewModel for Creator page - Flow visual node editor
    /// </summary>
    public partial class CreatorViewModel : ObservableObject, INavigationAware
    {
        private readonly FlowDocumentService _documentService;
        private readonly NodeCatalogService _catalogService;
        private readonly AppSettingsService _settingsService;

        private FlowDefinition _currentDefinition = null!;
        private bool _isInitialized = false;

        #region Status Properties

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private int _nodeCount;

        [ObservableProperty]
        private int _connectionCount;

        [ObservableProperty]
        private string _flowName = "New Flow";

        #endregion

        #region Canvas Properties

        [ObservableProperty]
        private FlowNodeViewModel? _selectedNode;

        [ObservableProperty]
        private FlowConnectionViewModel? _selectedConnection;

        [ObservableProperty]
        private string? _startNodeId;

        [ObservableProperty]
        private double _zoomLevel = 1.0;

        public ObservableCollection<FlowNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<FlowConnectionViewModel> Connections { get; } = new();

        [RelayCommand]
        private void ZoomIn()
        {
            ZoomLevel = Math.Min(3.0, ZoomLevel + 0.1);
        }

        [RelayCommand]
        private void ZoomOut()
        {
            ZoomLevel = Math.Max(0.2, ZoomLevel - 0.1);
        }

        [RelayCommand]
        private void ResetZoom()
        {
            ZoomLevel = 1.0;
        }

        #endregion

        #region Toolbox Properties

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private NodeCatalogEntry? _selectedCatalogEntry;

        public ObservableCollection<NodeCatalogCategory> Categories { get; } = new();
        public ObservableCollection<NodeCatalogCategory> FilteredCategories { get; } = new();

        #endregion

        #region Property Panel Properties

        [ObservableProperty]
        private bool _hasSelection;

        [ObservableProperty]
        private bool _isNodeSelected;

        [ObservableProperty]
        private bool _isConnectionSelected;

        #endregion

        public CreatorViewModel(
            FlowDocumentService documentService,
            NodeCatalogService catalogService,
            AppSettingsService settingsService)
        {
            _documentService = documentService;
            _catalogService = catalogService;
            _settingsService = settingsService;

            // Wire collection change handlers for status
            Nodes.CollectionChanged += (s, e) =>
            {
                NodeCount = Nodes.Count;
                OnPropertyChanged(nameof(NodeCount));
            };
            Connections.CollectionChanged += (s, e) =>
            {
                ConnectionCount = Connections.Count;
                OnPropertyChanged(nameof(ConnectionCount));
            };

            // Wire selection change handlers
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedNode))
                {
                    IsNodeSelected = SelectedNode != null;
                    HasSelection = SelectedNode != null || SelectedConnection != null;
                    UpdateNodeSelectionStates();
                }
                else if (e.PropertyName == nameof(SelectedConnection))
                {
                    IsConnectionSelected = SelectedConnection != null;
                    HasSelection = SelectedNode != null || SelectedConnection != null;
                    UpdateConnectionSelectionStates();
                }
            };
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
            NewFlow();
            _isInitialized = true;
        }

        #region Category/Toolbox Methods

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
                var filteredEntries = cat.Entries
                    .Where(e => e.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || e.Type.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || e.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filteredEntries.Count > 0)
                {
                    FilteredCategories.Add(new NodeCatalogCategory
                    {
                        Name = cat.Name,
                        Color = cat.Color,
                        Entries = filteredEntries
                    });
                }
            }
        }

        #endregion

        #region File Commands

        [RelayCommand]
        private void NewFlow()
        {
            _currentDefinition = _documentService.CreateNew();
            LoadFromDefinition(_currentDefinition);
            FlowName = _currentDefinition.Name;
            StatusText = "New flow created";
        }

        [RelayCommand]
        private void OpenFlow()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Flow JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Flow Definition"
            };

            var settings = _settingsService.Settings;
            if (!string.IsNullOrEmpty(settings.DefaultFlowDirectory))
            {
                var fullPath = _settingsService.ResolvePath(settings.DefaultFlowDirectory);
                if (Directory.Exists(fullPath))
                    dialog.InitialDirectory = fullPath;
            }

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var definition = _documentService.LoadFromFile(dialog.FileName);
                    if (definition != null)
                    {
                        _currentDefinition = definition;
                        LoadFromDefinition(definition);
                        FlowName = definition.Name;
                        StatusText = $"Loaded: {Path.GetFileName(dialog.FileName)}";
                    }
                    else
                    {
                        StatusText = "Failed to load flow file";
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Error loading: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private void SaveFlow()
        {
            UpdateDefinitionFromCanvas();

            if (_documentService.Save(_currentDefinition))
            {
                StatusText = $"Saved: {Path.GetFileName(_documentService.CurrentFilePath)}";
            }
            else
            {
                SaveAsFlow();
            }
        }

        [RelayCommand]
        private void SaveAsFlow()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Flow JSON Files (*.json)|*.json",
                Title = "Save Flow Definition",
                FileName = $"{_currentDefinition.Name}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                UpdateDefinitionFromCanvas();
                _documentService.SaveAs(_currentDefinition, dialog.FileName);
                StatusText = $"Saved: {Path.GetFileName(dialog.FileName)}";
            }
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            if (SelectedConnection != null)
            {
                Connections.Remove(SelectedConnection);
                SelectedConnection = null;
                StatusText = "Connection deleted";
                return;
            }

            // Multi-select delete
            var toDelete = SelectedNodes.Count > 0
                ? SelectedNodes.ToList()
                : (SelectedNode != null ? new List<FlowNodeViewModel> { SelectedNode } : new List<FlowNodeViewModel>());

            if (toDelete.Count == 0) return;

            var deletedIds = new HashSet<string>(toDelete.Select(n => n.Id));

            // Remove all connections to/from deleted nodes
            var related = Connections
                .Where(c => deletedIds.Contains(c.SourceNodeId) || deletedIds.Contains(c.TargetNodeId))
                .ToList();
            foreach (var conn in related)
                Connections.Remove(conn);

            // Reassign start node if needed
            bool startDeleted = toDelete.Any(n => n.IsStartNode);

            foreach (var node in toDelete)
                Nodes.Remove(node);

            if (startDeleted && Nodes.Count > 0)
            {
                var next = Nodes.First();
                StartNodeId = next.Id;
                next.IsStartNode = true;
            }

            ClearSelection();
            SelectedNode = null;
            StatusText = $"Deleted {toDelete.Count} node(s), {related.Count} connection(s)";
        }

        private void UpdateDefinitionFromCanvas()
        {
            var def = ToDefinition(_currentDefinition.Name, _currentDefinition.Description);
            def.Id = _currentDefinition.Id;
            def.CreatedAt = _currentDefinition.CreatedAt;
            def.Version = _currentDefinition.Version;
            _currentDefinition = def;
        }

        #endregion

        #region Canvas Methods

        /// <summary>
        /// Load from a FlowDefinition
        /// </summary>
        public void LoadFromDefinition(FlowDefinition definition)
        {
            Nodes.Clear();
            Connections.Clear();
            StartNodeId = definition.StartNodeId;

            // Create node VMs
            foreach (var nodeDef in definition.Nodes)
            {
                var entry = _catalogService.FindEntry(nodeDef.Type);
                var nodeVm = FlowNodeViewModel.FromDefinition(nodeDef, entry);
                nodeVm.IsStartNode = nodeDef.Id == definition.StartNodeId;
                Nodes.Add(nodeVm);
            }

            // Create connection VMs and resolve node references
            foreach (var connDef in definition.Connections)
            {
                var connVm = FlowConnectionViewModel.FromDefinition(connDef);
                connVm.SourceNode = Nodes.FirstOrDefault(n => n.Id == connDef.SourceNodeId);
                connVm.TargetNode = Nodes.FirstOrDefault(n => n.Id == connDef.TargetNodeId);
                Connections.Add(connVm);
            }
        }

        /// <summary>
        /// Export to FlowDefinition for serialization
        /// </summary>
        public FlowDefinition ToDefinition(string name = "Flow", string? description = null)
        {
            var definition = new FlowDefinition
            {
                Name = name,
                Description = description,
                StartNodeId = StartNodeId,
                ModifiedAt = DateTime.Now
            };

            foreach (var node in Nodes)
                definition.Nodes.Add(node.ToDefinition());

            foreach (var conn in Connections)
                definition.Connections.Add(conn.ToDefinition());

            return definition;
        }

        /// <summary>
        /// Add a new node at specified canvas position
        /// </summary>
        public FlowNodeViewModel AddNode(NodeCatalogEntry entry, double x, double y)
        {
            var nodeDef = new NodeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Type = entry.Type,
                Name = entry.DisplayName,
                X = x,
                Y = y
            };

            // Populate default properties from input definitions
            foreach (var input in entry.InputDefinitions)
            {
                if (input.DefaultValue != null)
                    nodeDef.Properties[input.Name] = input.DefaultValue.ToString() ?? string.Empty;
            }

            var nodeVm = FlowNodeViewModel.FromDefinition(nodeDef, entry);
            Nodes.Add(nodeVm);

            // If first node, set as start
            if (Nodes.Count == 1)
            {
                StartNodeId = nodeVm.Id;
                nodeVm.IsStartNode = true;
            }

            return nodeVm;
        }

        /// <summary>
        /// Add a connection between two nodes
        /// </summary>
        public FlowConnectionViewModel? AddConnection(string sourceNodeId, string targetNodeId, string? condition = null)
        {
            // Prevent duplicate connections
            if (Connections.Any(c => c.SourceNodeId == sourceNodeId && c.TargetNodeId == targetNodeId))
                return null;

            // Prevent self-connections
            if (sourceNodeId == targetNodeId)
                return null;

            var sourceNode = Nodes.FirstOrDefault(n => n.Id == sourceNodeId);
            var targetNode = Nodes.FirstOrDefault(n => n.Id == targetNodeId);
            if (sourceNode == null || targetNode == null)
                return null;

            var conn = new FlowConnectionViewModel
            {
                Id = Guid.NewGuid().ToString(),
                SourceNodeId = sourceNodeId,
                TargetNodeId = targetNodeId,
                Condition = condition,
                SourceNode = sourceNode,
                TargetNode = targetNode
            };

            Connections.Add(conn);
            return conn;
        }

        #endregion

        #region Selection Methods

        /// <summary>
        /// Tracks all currently selected nodes (for multi-select)
        /// </summary>
        public HashSet<FlowNodeViewModel> SelectedNodes { get; } = new();

        [RelayCommand]
        private void SetStartNode(FlowNodeViewModel node)
        {
            foreach (var n in Nodes)
                n.IsStartNode = false;

            node.IsStartNode = true;
            StartNodeId = node.Id;
        }

        /// <summary>
        /// Select a single node (clears previous selection unless additive)
        /// </summary>
        public void SelectNode(FlowNodeViewModel? node, bool additive = false)
        {
            SelectedConnection = null;

            if (node == null)
            {
                // Clear all
                ClearSelection();
                SelectedNode = null;
                return;
            }

            if (additive)
            {
                // Toggle in multi-select set
                if (SelectedNodes.Contains(node))
                {
                    SelectedNodes.Remove(node);
                    node.IsSelected = false;
                    // Update primary selected node
                    SelectedNode = SelectedNodes.LastOrDefault();
                }
                else
                {
                    SelectedNodes.Add(node);
                    node.IsSelected = true;
                    SelectedNode = node;
                }
            }
            else
            {
                // Single select — clear previous
                ClearSelection();
                SelectedNodes.Add(node);
                node.IsSelected = true;
                SelectedNode = node;
            }

            HasSelection = SelectedNodes.Count > 0 || SelectedConnection != null;
            IsNodeSelected = SelectedNode != null;
            UpdateStatusText();
        }

        public void SelectConnection(FlowConnectionViewModel? connection)
        {
            ClearSelection();
            SelectedNode = null;
            SelectedConnection = connection;
        }

        /// <summary>
        /// Select all nodes on canvas
        /// </summary>
        [RelayCommand]
        private void SelectAll()
        {
            SelectedConnection = null;
            SelectedNodes.Clear();
            foreach (var n in Nodes)
            {
                n.IsSelected = true;
                SelectedNodes.Add(n);
            }
            SelectedNode = Nodes.LastOrDefault();
            HasSelection = SelectedNodes.Count > 0;
            IsNodeSelected = SelectedNode != null;
            UpdateStatusText();
        }

        /// <summary>
        /// Add nodes within a rectangle to selection
        /// </summary>
        public void SelectNodesInRect(Rect rect, bool additive = false)
        {
            if (!additive)
            {
                ClearSelection();
            }

            foreach (var node in Nodes)
            {
                var nodeBounds = new Rect(node.X, node.Y, node.Width, node.Height);
                if (rect.IntersectsWith(nodeBounds))
                {
                    SelectedNodes.Add(node);
                    node.IsSelected = true;
                }
            }

            SelectedNode = SelectedNodes.LastOrDefault();
            HasSelection = SelectedNodes.Count > 0;
            IsNodeSelected = SelectedNode != null;
            UpdateStatusText();
        }

        /// <summary>
        /// Flip selected nodes (swap input/output port sides)
        /// </summary>
        [RelayCommand]
        private void FlipSelected()
        {
            if (SelectedNodes.Count == 0 && SelectedNode != null)
                SelectedNodes.Add(SelectedNode);

            foreach (var node in SelectedNodes)
                node.IsFlipped = !node.IsFlipped;

            StatusText = $"Flipped {SelectedNodes.Count} node(s)";
        }

        private void ClearSelection()
        {
            foreach (var n in SelectedNodes)
                n.IsSelected = false;
            SelectedNodes.Clear();
            foreach (var c in Connections)
                c.IsSelected = false;
        }

        private void UpdateNodeSelectionStates()
        {
            foreach (var n in Nodes)
                n.IsSelected = SelectedNodes.Contains(n);
            foreach (var c in Connections)
                c.IsSelected = false;
        }

        private void UpdateConnectionSelectionStates()
        {
            foreach (var n in Nodes)
                n.IsSelected = false;
            foreach (var c in Connections)
                c.IsSelected = (c == SelectedConnection);
        }

        private void UpdateStatusText()
        {
            if (SelectedNodes.Count > 1)
                StatusText = $"{SelectedNodes.Count} nodes selected";
            else if (SelectedNode != null)
                StatusText = $"Selected: {SelectedNode.Name}";
            else if (SelectedConnection != null)
                StatusText = "Connection selected";
            else
                StatusText = "Ready";
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for a single flow node on the canvas
    /// </summary>
    public partial class FlowNodeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _nodeType = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private double _x;

        [ObservableProperty]
        private double _y;

        [ObservableProperty]
        private double _width = 160;

        [ObservableProperty]
        private double _height = 80;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _color = "#2196F3";

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isStartNode;

        [ObservableProperty]
        private bool _isFlipped;

        public ObservableCollection<NodePropertyViewModel> Properties { get; } = new();
        public ObservableCollection<FlowParameter> InputDefinitions { get; } = new();
        public ObservableCollection<FlowParameter> OutputDefinitions { get; } = new();

        public static FlowNodeViewModel FromDefinition(NodeDefinition def, NodeCatalogEntry? catalogEntry)
        {
            var vm = new FlowNodeViewModel
            {
                Id = def.Id,
                Name = def.Name,
                NodeType = def.Type,
                Description = def.Description,
                X = def.X,
                Y = def.Y,
                IsFlipped = def.IsFlipped,
                Width = catalogEntry?.DefaultWidth ?? 160,
                Height = catalogEntry?.DefaultHeight ?? 80,
                Category = catalogEntry?.Category ?? "Unknown",
                Color = catalogEntry?.Color ?? "#2196F3"
            };

            if (catalogEntry != null)
            {
                foreach (var input in catalogEntry.InputDefinitions)
                    vm.InputDefinitions.Add(input);
                foreach (var output in catalogEntry.OutputDefinitions)
                    vm.OutputDefinitions.Add(output);
            }

            foreach (var kvp in def.Properties)
            {
                vm.Properties.Add(new NodePropertyViewModel
                {
                    Name = kvp.Key,
                    Value = kvp.Value,
                    ParameterDef = vm.InputDefinitions.FirstOrDefault(i => i.Name == kvp.Key)
                });
            }

            if (catalogEntry != null)
            {
                foreach (var input in catalogEntry.InputDefinitions)
                {
                    if (!def.Properties.ContainsKey(input.Name))
                    {
                        vm.Properties.Add(new NodePropertyViewModel
                        {
                            Name = input.Name,
                            Value = input.DefaultValue?.ToString() ?? string.Empty,
                            ParameterDef = input
                        });
                    }
                }
            }

            return vm;
        }

        public NodeDefinition ToDefinition()
        {
            var def = new NodeDefinition
            {
                Id = Id,
                Type = NodeType,
                Name = Name,
                Description = Description,
                X = X,
                Y = Y,
                IsFlipped = IsFlipped
            };

            foreach (var prop in Properties)
            {
                def.Properties[prop.Name] = prop.Value;
            }

            return def;
        }
    }

    public partial class NodePropertyViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        public FlowParameter? ParameterDef { get; set; }

        public string DisplayName => ParameterDef?.DisplayName ?? Name;
        public string Type => ParameterDef?.Type ?? "string";
        public bool IsRequired => ParameterDef?.Required ?? false;
        public string? ParameterDescription => ParameterDef?.Description;
    }

    /// <summary>
    /// ViewModel for a connection line between two nodes
    /// </summary>
    public partial class FlowConnectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _sourceNodeId = string.Empty;

        [ObservableProperty]
        private string _targetNodeId = string.Empty;

        [ObservableProperty]
        private string? _condition;

        [ObservableProperty]
        private bool _isSelected;

        private FlowNodeViewModel? _sourceNode;
        private FlowNodeViewModel? _targetNode;

        public FlowNodeViewModel? SourceNode
        {
            get => _sourceNode;
            set
            {
                _sourceNode = value;
                if (value != null)
                {
                    value.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName is nameof(FlowNodeViewModel.X) or nameof(FlowNodeViewModel.Y)
                            or nameof(FlowNodeViewModel.Width) or nameof(FlowNodeViewModel.Height)
                            or nameof(FlowNodeViewModel.IsFlipped))
                        {
                            OnPropertyChanged(nameof(SourceX));
                            OnPropertyChanged(nameof(SourceY));
                            OnPropertyChanged(nameof(MidX));
                            OnPropertyChanged(nameof(MidY));
                            OnPropertyChanged(nameof(PathData));
                        }
                    };
                }
            }
        }

        public FlowNodeViewModel? TargetNode
        {
            get => _targetNode;
            set
            {
                _targetNode = value;
                if (value != null)
                {
                    value.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName is nameof(FlowNodeViewModel.X) or nameof(FlowNodeViewModel.Y)
                            or nameof(FlowNodeViewModel.Width) or nameof(FlowNodeViewModel.Height)
                            or nameof(FlowNodeViewModel.IsFlipped))
                        {
                            OnPropertyChanged(nameof(TargetX));
                            OnPropertyChanged(nameof(TargetY));
                            OnPropertyChanged(nameof(MidX));
                            OnPropertyChanged(nameof(MidY));
                            OnPropertyChanged(nameof(PathData));
                        }
                    };
                }
            }
        }

        // Output port: right side normally, left side if flipped
        public double SourceX => (SourceNode?.IsFlipped == true)
            ? (SourceNode?.X ?? 0)
            : (SourceNode?.X ?? 0) + (SourceNode?.Width ?? 160);
        public double SourceY => (SourceNode?.Y ?? 0) + (SourceNode?.Height ?? 80) / 2;
        // Input port: left side normally, right side if flipped
        public double TargetX => (TargetNode?.IsFlipped == true)
            ? (TargetNode?.X ?? 0) + (TargetNode?.Width ?? 160)
            : (TargetNode?.X ?? 0);
        public double TargetY => (TargetNode?.Y ?? 0) + (TargetNode?.Height ?? 80) / 2;
        public double MidX => (SourceX + TargetX) / 2;
        public double MidY => (SourceY + TargetY) / 2;

        public string PathData
        {
            get
            {
                var sx = SourceX;
                var sy = SourceY;
                var tx = TargetX;
                var ty = TargetY;
                var dx = Math.Abs(tx - sx) * 0.5;
                return $"M {sx},{sy} C {sx + dx},{sy} {tx - dx},{ty} {tx},{ty}";
            }
        }

        public static FlowConnectionViewModel FromDefinition(ConnectionDefinition def)
        {
            return new FlowConnectionViewModel
            {
                Id = def.Id,
                SourceNodeId = def.SourceNodeId,
                TargetNodeId = def.TargetNodeId,
                Condition = def.Condition
            };
        }

        public ConnectionDefinition ToDefinition()
        {
            return new ConnectionDefinition
            {
                Id = Id,
                SourceNodeId = SourceNodeId,
                TargetNodeId = TargetNodeId,
                Condition = Condition
            };
        }
    }
}
