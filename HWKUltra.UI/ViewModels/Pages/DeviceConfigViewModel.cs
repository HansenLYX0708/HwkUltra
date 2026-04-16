using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using Wpf.Ui.Abstractions.Controls;
using HWKUltra.UI.Models;

namespace HWKUltra.UI.ViewModels.Pages
{
    /// <summary>
    /// ViewModel for Device Configuration page.
    /// Dynamically loads JSON config files and renders editable property trees.
    /// </summary>
    public partial class DeviceConfigViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized;

        /// <summary>
        /// ConfigJson base directory under exe output folder
        /// </summary>
        private static string ConfigJsonBaseDir
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigJson");

        #region Device Type Selection

        /// <summary>
        /// Available device types (auto-discovered from ConfigJson subfolders)
        /// </summary>
        public ObservableCollection<DeviceTypeInfo> DeviceTypes { get; } = new();

        [ObservableProperty]
        private DeviceTypeInfo? _selectedDeviceType;

        /// <summary>
        /// Available config files for the selected device type
        /// </summary>
        public ObservableCollection<string> AvailableConfigs { get; } = new();

        [ObservableProperty]
        private string? _selectedConfigFile;

        #endregion

        #region Config Editor State

        /// <summary>
        /// Dynamic property tree loaded from JSON
        /// </summary>
        public ObservableCollection<JsonPropertyModel> Properties { get; } = new();

        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        [ObservableProperty]
        private bool _isModified;

        [ObservableProperty]
        private bool _isConfigLoaded;

        [ObservableProperty]
        private string _statusMessage = "Select a device type to begin";

        public ObservableCollection<string> ValidationLog { get; } = new();

        #endregion

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                Initialize();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void Initialize()
        {
            DiscoverDeviceTypes();
            _isInitialized = true;
        }

        /// <summary>
        /// Scan ConfigJson subfolders to discover available device types and their default configs
        /// </summary>
        private void DiscoverDeviceTypes()
        {
            DeviceTypes.Clear();

            if (!Directory.Exists(ConfigJsonBaseDir))
            {
                StatusMessage = $"ConfigJson folder not found: {ConfigJsonBaseDir}";
                return;
            }

            foreach (var dir in Directory.GetDirectories(ConfigJsonBaseDir))
            {
                var folderName = Path.GetFileName(dir);
                // Skip non-device folders
                if (folderName.Equals("Flow", StringComparison.OrdinalIgnoreCase) ||
                    folderName.Equals("configs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var jsonFiles = Directory.GetFiles(dir, "*.json");
                if (jsonFiles.Length > 0)
                {
                    DeviceTypes.Add(new DeviceTypeInfo
                    {
                        Name = folderName,
                        FolderPath = dir,
                        DefaultFiles = jsonFiles.Select(Path.GetFileName).ToList()!
                    });
                }
            }

            StatusMessage = $"Found {DeviceTypes.Count} device types";
        }

        partial void OnSelectedDeviceTypeChanged(DeviceTypeInfo? value)
        {
            AvailableConfigs.Clear();
            SelectedConfigFile = null;

            if (value == null) return;

            foreach (var file in value.DefaultFiles)
                AvailableConfigs.Add(file);

            StatusMessage = $"{value.Name}: {value.DefaultFiles.Count} config file(s) available";
        }

        partial void OnSelectedConfigFileChanged(string? value)
        {
            if (value == null || SelectedDeviceType == null) return;

            var path = Path.Combine(SelectedDeviceType.FolderPath, value);
            LoadConfigFromFile(path);
        }

        #region Commands

        [RelayCommand]
        private void NewConfig()
        {
            if (SelectedDeviceType == null)
            {
                StatusMessage = "Select a device type first";
                return;
            }

            // Use the first default file as template
            if (SelectedDeviceType.DefaultFiles.Count > 0)
            {
                var templatePath = Path.Combine(SelectedDeviceType.FolderPath, SelectedDeviceType.DefaultFiles[0]);
                LoadConfigFromFile(templatePath);
                CurrentFilePath = string.Empty; // Mark as new (no saved path yet)
                IsModified = true;
                StatusMessage = $"New config from template: {SelectedDeviceType.DefaultFiles[0]}";
            }
            else
            {
                // Empty config
                Properties.Clear();
                CurrentFilePath = string.Empty;
                IsConfigLoaded = true;
                IsModified = true;
                StatusMessage = "New empty config";
            }
        }

        [RelayCommand]
        private void ImportConfig()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Import Device Config"
            };

            if (SelectedDeviceType != null)
                dialog.InitialDirectory = SelectedDeviceType.FolderPath;

            if (dialog.ShowDialog() == true)
            {
                LoadConfigFromFile(dialog.FileName);
                StatusMessage = $"Imported: {Path.GetFileName(dialog.FileName)}";
            }
        }

        [RelayCommand]
        private void SaveConfig()
        {
            if (!IsConfigLoaded) return;

            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                // No path yet — redirect to SaveAs
                SaveConfigAs();
                return;
            }

            WriteConfigToFile(CurrentFilePath);
        }

        [RelayCommand]
        private void SaveConfigAs()
        {
            if (!IsConfigLoaded) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Device Config As"
            };

            if (SelectedDeviceType != null)
                dialog.InitialDirectory = SelectedDeviceType.FolderPath;

            if (!string.IsNullOrEmpty(CurrentFilePath))
                dialog.FileName = Path.GetFileName(CurrentFilePath);

            if (dialog.ShowDialog() == true)
            {
                WriteConfigToFile(dialog.FileName);
                CurrentFilePath = dialog.FileName;

                // Refresh available configs if saved into device folder
                if (SelectedDeviceType != null)
                    RefreshAvailableConfigs();
            }
        }

        [RelayCommand]
        private void AddArrayItem(JsonPropertyModel parent)
        {
            if (parent.PropType != JsonPropType.Array) return;

            JsonPropertyModel newItem;
            if (parent.Children.Count > 0)
            {
                // Clone last item as template
                var template = parent.Children[parent.Children.Count - 1];
                newItem = template.DeepClone();
            }
            else
            {
                // Empty object
                newItem = new JsonPropertyModel { PropType = JsonPropType.Object };
            }

            newItem.ArrayIndex = parent.Children.Count;
            parent.Children.Add(newItem);
            IsModified = true;
        }

        [RelayCommand]
        private void RemoveArrayItem(JsonPropertyModel item)
        {
            // Find parent and remove
            foreach (var prop in Properties)
            {
                if (TryRemoveFromParent(prop, item))
                {
                    IsModified = true;
                    return;
                }
            }
        }

        [RelayCommand]
        private void ValidateConfig()
        {
            ValidationLog.Clear();

            if (!IsConfigLoaded || Properties.Count == 0)
            {
                ValidationLog.Add("[WARN] No config loaded");
                return;
            }

            try
            {
                var json = JsonPropertyModel.ToJsonString(Properties);
                var doc = JsonDocument.Parse(json);

                ValidationLog.Add($"[OK] JSON is valid ({doc.RootElement.EnumerateObject().Count()} top-level keys)");

                // Check for empty required fields
                ValidateProperties(Properties, "", ValidationLog);

                if (ValidationLog.All(l => l.StartsWith("[OK]")))
                    ValidationLog.Add("[OK] All validations passed");
            }
            catch (Exception ex)
            {
                ValidationLog.Add($"[ERROR] JSON serialization failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void RefreshDeviceTypes()
        {
            DiscoverDeviceTypes();
        }

        #endregion

        #region Helpers

        private void LoadConfigFromFile(string path)
        {
            Properties.Clear();
            ValidationLog.Clear();

            try
            {
                var json = File.ReadAllText(path);
                var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    StatusMessage = "Error: JSON root must be an object";
                    return;
                }

                var props = JsonPropertyModel.FromJsonElement(doc.RootElement);
                foreach (var p in props)
                    Properties.Add(p);

                CurrentFilePath = path;
                IsConfigLoaded = true;
                IsModified = false;
                StatusMessage = $"Loaded: {Path.GetFileName(path)} ({Properties.Count} top-level keys)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load error: {ex.Message}";
                IsConfigLoaded = false;
            }
        }

        private void WriteConfigToFile(string path)
        {
            try
            {
                var json = JsonPropertyModel.ToJsonString(Properties);
                File.WriteAllText(path, json);
                IsModified = false;
                StatusMessage = $"Saved: {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save error: {ex.Message}";
            }
        }

        private void RefreshAvailableConfigs()
        {
            if (SelectedDeviceType == null) return;

            var jsonFiles = Directory.GetFiles(SelectedDeviceType.FolderPath, "*.json");
            SelectedDeviceType.DefaultFiles = jsonFiles.Select(Path.GetFileName).ToList()!;

            var current = SelectedConfigFile;
            AvailableConfigs.Clear();
            foreach (var file in SelectedDeviceType.DefaultFiles)
                AvailableConfigs.Add(file);

            if (current != null && AvailableConfigs.Contains(current))
                SelectedConfigFile = current;
        }

        private static void ValidateProperties(ObservableCollection<JsonPropertyModel> props, string path, ObservableCollection<string> log)
        {
            foreach (var prop in props)
            {
                var fullPath = string.IsNullOrEmpty(path)
                    ? prop.DisplayKey
                    : $"{path}.{prop.DisplayKey}";

                switch (prop.PropType)
                {
                    case JsonPropType.String when string.IsNullOrEmpty(prop.Value):
                        log.Add($"[WARN] {fullPath}: Empty string value");
                        break;

                    case JsonPropType.Number when !double.TryParse(prop.Value,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _):
                        log.Add($"[ERROR] {fullPath}: Invalid number '{prop.Value}'");
                        break;

                    case JsonPropType.Object:
                    case JsonPropType.Array:
                        ValidateProperties(prop.Children, fullPath, log);
                        break;
                }
            }
        }

        private static bool TryRemoveFromParent(JsonPropertyModel parent, JsonPropertyModel target)
        {
            if (parent.Children.Contains(target))
            {
                parent.Children.Remove(target);
                // Re-index array elements
                if (parent.PropType == JsonPropType.Array)
                {
                    for (int i = 0; i < parent.Children.Count; i++)
                        parent.Children[i].ArrayIndex = i;
                }
                return true;
            }

            foreach (var child in parent.Children)
            {
                if (TryRemoveFromParent(child, target))
                    return true;
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Represents a discovered device type with its config files
    /// </summary>
    public class DeviceTypeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public List<string> DefaultFiles { get; set; } = new();

        public override string ToString() => Name;
    }
}
