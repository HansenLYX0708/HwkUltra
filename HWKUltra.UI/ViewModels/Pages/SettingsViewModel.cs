using System.IO;
using Microsoft.Win32;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using HWKUltra.UI.Services;

namespace HWKUltra.UI.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly AppSettingsService _settingsService;
        private readonly NodeCatalogService _catalogService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        [ObservableProperty]
        private string _nodeCatalogConfigPath = string.Empty;

        [ObservableProperty]
        private string _defaultFlowDirectory = string.Empty;

        [ObservableProperty]
        private double _canvasGridSize = 20;

        [ObservableProperty]
        private bool _canvasSnapToGrid = true;

        public SettingsViewModel(AppSettingsService settingsService, NodeCatalogService catalogService)
        {
            _settingsService = settingsService;
            _catalogService = catalogService;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            // Auto-save when leaving settings page
            SaveSettings();
            return Task.CompletedTask;
        }

        private void InitializeViewModel()
        {
            var settings = _settingsService.Settings;

            CurrentTheme = settings.Theme == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            AppVersion = $"HWKUltra.UI - {GetAssemblyVersion()}";
            NodeCatalogConfigPath = settings.NodeCatalogConfigPath;
            DefaultFlowDirectory = settings.DefaultFlowDirectory;
            CanvasGridSize = settings.CanvasGridSize;
            CanvasSnapToGrid = settings.CanvasSnapToGrid;

            _isInitialized = true;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;
                    _settingsService.Settings.Theme = "Light";
                    break;

                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;
                    _settingsService.Settings.Theme = "Dark";
                    break;
            }

            _settingsService.Save();
        }

        [RelayCommand]
        private void BrowseNodeCatalogConfig()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Select Node Catalog Config File"
            };

            var currentPath = _settingsService.ResolvePath(NodeCatalogConfigPath);
            if (File.Exists(currentPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                dialog.FileName = Path.GetFileName(currentPath);
            }

            if (dialog.ShowDialog() == true)
            {
                // Store as relative path if under app directory
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var fullPath = dialog.FileName;
                NodeCatalogConfigPath = fullPath.StartsWith(basePath)
                    ? fullPath[basePath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    : fullPath;

                _settingsService.Settings.NodeCatalogConfigPath = NodeCatalogConfigPath;
                _settingsService.Save();
                _catalogService.InvalidateCache();
            }
        }

        [RelayCommand]
        private void BrowseDefaultFlowDirectory()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Default Flow Directory"
            };

            var currentPath = _settingsService.ResolvePath(DefaultFlowDirectory);
            if (Directory.Exists(currentPath))
            {
                dialog.InitialDirectory = currentPath;
            }

            if (dialog.ShowDialog() == true && dialog.FolderName is string dir)
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                DefaultFlowDirectory = dir.StartsWith(basePath)
                    ? dir[basePath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    : dir;

                _settingsService.Settings.DefaultFlowDirectory = DefaultFlowDirectory;
                _settingsService.Save();
            }
        }

        partial void OnCanvasGridSizeChanged(double value)
        {
            if (_isInitialized)
            {
                _settingsService.Settings.CanvasGridSize = value;
                _settingsService.Save();
            }
        }

        partial void OnCanvasSnapToGridChanged(bool value)
        {
            if (_isInitialized)
            {
                _settingsService.Settings.CanvasSnapToGrid = value;
                _settingsService.Save();
            }
        }

        private void SaveSettings()
        {
            if (!_isInitialized) return;

            _settingsService.Settings.NodeCatalogConfigPath = NodeCatalogConfigPath;
            _settingsService.Settings.DefaultFlowDirectory = DefaultFlowDirectory;
            _settingsService.Settings.CanvasGridSize = CanvasGridSize;
            _settingsService.Settings.CanvasSnapToGrid = CanvasSnapToGrid;
            _settingsService.Save();
        }
    }
}
