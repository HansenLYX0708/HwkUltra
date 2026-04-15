using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using HWKUltra.Flow.Models;
using HWKUltra.Creator.Services;

namespace HWKUltra.Creator.ViewModels
{
    /// <summary>
    /// Main window ViewModel - orchestrates all sub-VMs and commands
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly FlowDocumentService _documentService;
        private readonly NodeCatalogService _catalogService;
        private readonly EditorConfigService _configService;

        private FlowDefinition _currentDefinition;

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private int _nodeCount;

        [ObservableProperty]
        private int _connectionCount;

        [ObservableProperty]
        private string _flowName = "New Flow";

        public FlowCanvasViewModel Canvas { get; }
        public NodeToolboxViewModel Toolbox { get; }
        public PropertyPanelViewModel PropertyPanel { get; }

        public MainWindowViewModel(
            FlowDocumentService documentService,
            NodeCatalogService catalogService,
            EditorConfigService configService,
            FlowCanvasViewModel canvas,
            NodeToolboxViewModel toolbox,
            PropertyPanelViewModel propertyPanel)
        {
            _documentService = documentService;
            _catalogService = catalogService;
            _configService = configService;
            Canvas = canvas;
            Toolbox = toolbox;
            PropertyPanel = propertyPanel;

            // Wire selection changes
            Canvas.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FlowCanvasViewModel.SelectedNode))
                {
                    PropertyPanel.UpdateSelection(Canvas.SelectedNode, null);
                }
                else if (e.PropertyName == nameof(FlowCanvasViewModel.SelectedConnection))
                {
                    PropertyPanel.UpdateSelection(null, Canvas.SelectedConnection);
                }
            };

            Canvas.Nodes.CollectionChanged += (s, e) => NodeCount = Canvas.Nodes.Count;
            Canvas.Connections.CollectionChanged += (s, e) => ConnectionCount = Canvas.Connections.Count;

            // Create initial empty flow
            _currentDefinition = _documentService.CreateNew();
            FlowName = _currentDefinition.Name;
        }

        [RelayCommand]
        private void NewFlow()
        {
            _currentDefinition = _documentService.CreateNew();
            Canvas.LoadFromDefinition(_currentDefinition);
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

            var config = _configService.GetConfig();
            if (!string.IsNullOrEmpty(config.DefaultFlowDirectory))
            {
                var fullPath = Path.GetFullPath(config.DefaultFlowDirectory);
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
                        Canvas.LoadFromDefinition(definition);
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
                // No file path yet, redirect to Save As
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
            Canvas.DeleteSelectedCommand.Execute(null);
        }

        private void UpdateDefinitionFromCanvas()
        {
            var def = Canvas.ToDefinition(_currentDefinition.Name, _currentDefinition.Description);
            def.Id = _currentDefinition.Id;
            def.CreatedAt = _currentDefinition.CreatedAt;
            def.Version = _currentDefinition.Version;
            _currentDefinition = def;
        }
    }
}
