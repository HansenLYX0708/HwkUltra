using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using Wpf.Ui.Abstractions.Controls;
using HWKUltra.Core;

namespace HWKUltra.UI.ViewModels.Pages
{
    /// <summary>
    /// ViewModel for the Teach Data management page.
    /// Displays groups and positions, allows editing axis values, adding/removing positions, and saving.
    /// </summary>
    public partial class TeachDataViewModel : ObservableObject, INavigationAware
    {
        private readonly TeachDataService _teachDataService = new();
        private bool _isInitialized;

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            // Try to load default teach data
            var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigJson", "TeachData", "TeachData.json");
            if (File.Exists(defaultPath))
            {
                LoadFile(defaultPath);
            }
            _isInitialized = true;
        }

        #region Properties

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private ObservableCollection<TeachGroupViewModel> _groups = new();

        [ObservableProperty]
        private TeachGroupViewModel? _selectedGroup;

        [ObservableProperty]
        private ObservableCollection<TeachPositionViewModel> _filteredPositions = new();

        [ObservableProperty]
        private TeachPositionViewModel? _selectedPosition;

        [ObservableProperty]
        private ObservableCollection<AxisValueViewModel> _selectedAxes = new();

        #endregion

        #region Commands

        [RelayCommand]
        private void Open()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Teach Data File",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigJson", "TeachData")
            };

            if (dialog.ShowDialog() == true)
            {
                LoadFile(dialog.FileName);
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                SaveAs();
                return;
            }

            ApplyChangesToService();
            _teachDataService.Save(FilePath);
            IsDirty = false;
            StatusMessage = $"Saved to {Path.GetFileName(FilePath)}";
        }

        [RelayCommand]
        private void SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Save Teach Data File",
                FileName = "TeachData.json",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigJson", "TeachData")
            };

            if (dialog.ShowDialog() == true)
            {
                ApplyChangesToService();
                _teachDataService.Save(dialog.FileName);
                FilePath = dialog.FileName;
                IsDirty = false;
                StatusMessage = $"Saved to {Path.GetFileName(dialog.FileName)}";
            }
        }

        [RelayCommand]
        private void AddGroup()
        {
            var name = $"NewGroup{Groups.Count + 1}";
            var groupVm = new TeachGroupViewModel
            {
                Name = name,
                Description = "",
                RequiredAxes = "X, Y, Z"
            };
            Groups.Add(groupVm);
            SelectedGroup = groupVm;
            IsDirty = true;
            StatusMessage = $"Added group: {name}";
        }

        [RelayCommand]
        private void RemoveGroup()
        {
            if (SelectedGroup == null) return;
            var name = SelectedGroup.Name;
            Groups.Remove(SelectedGroup);
            SelectedGroup = Groups.FirstOrDefault();
            IsDirty = true;
            StatusMessage = $"Removed group: {name}";
        }

        [RelayCommand]
        private void AddPosition()
        {
            var group = SelectedGroup?.Name ?? "Default";
            var name = $"NewPosition{FilteredPositions.Count + 1}";
            var posVm = new TeachPositionViewModel
            {
                Name = name,
                Group = group,
                Description = ""
            };

            // Add default axes from group's required axes
            if (SelectedGroup != null && !string.IsNullOrEmpty(SelectedGroup.RequiredAxes))
            {
                var axes = SelectedGroup.RequiredAxes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var axis in axes)
                {
                    posVm.Axes.Add(new AxisValueViewModel { AxisName = axis, Value = 0.0 });
                }
            }
            else
            {
                posVm.Axes.Add(new AxisValueViewModel { AxisName = "X", Value = 0.0 });
                posVm.Axes.Add(new AxisValueViewModel { AxisName = "Y", Value = 0.0 });
                posVm.Axes.Add(new AxisValueViewModel { AxisName = "Z", Value = 0.0 });
            }

            FilteredPositions.Add(posVm);
            SelectedPosition = posVm;
            IsDirty = true;
            StatusMessage = $"Added position: {name}";
        }

        [RelayCommand]
        private void RemovePosition()
        {
            if (SelectedPosition == null) return;
            var name = SelectedPosition.Name;
            FilteredPositions.Remove(SelectedPosition);
            SelectedPosition = FilteredPositions.FirstOrDefault();
            IsDirty = true;
            StatusMessage = $"Removed position: {name}";
        }

        [RelayCommand]
        private void AddAxis()
        {
            if (SelectedPosition == null) return;
            SelectedAxes.Add(new AxisValueViewModel { AxisName = "NewAxis", Value = 0.0 });
            IsDirty = true;
        }

        [RelayCommand]
        private void RemoveAxis(AxisValueViewModel? axis)
        {
            if (axis == null || SelectedPosition == null) return;
            SelectedAxes.Remove(axis);
            SelectedPosition.Axes.Remove(axis);
            IsDirty = true;
        }

        [RelayCommand]
        private void DuplicatePosition()
        {
            if (SelectedPosition == null) return;
            var src = SelectedPosition;
            var posVm = new TeachPositionViewModel
            {
                Name = src.Name + "_Copy",
                Group = src.Group,
                Description = src.Description
            };
            foreach (var axis in src.Axes)
            {
                posVm.Axes.Add(new AxisValueViewModel { AxisName = axis.AxisName, Value = axis.Value });
            }
            FilteredPositions.Add(posVm);
            SelectedPosition = posVm;
            IsDirty = true;
            StatusMessage = $"Duplicated: {src.Name} → {posVm.Name}";
        }

        #endregion

        #region Partial Property Changed

        partial void OnSelectedGroupChanged(TeachGroupViewModel? value)
        {
            RefreshFilteredPositions();
        }

        partial void OnSelectedPositionChanged(TeachPositionViewModel? value)
        {
            if (value != null)
            {
                SelectedAxes = value.Axes;
            }
            else
            {
                SelectedAxes = new ObservableCollection<AxisValueViewModel>();
            }
        }

        #endregion

        #region Internal

        private void LoadFile(string path)
        {
            try
            {
                _teachDataService.Load(path);
                FilePath = path;
                RefreshFromService();
                IsDirty = false;
                StatusMessage = $"Loaded: {Path.GetFileName(path)} ({_teachDataService.GetPositionNames().Count} positions)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load error: {ex.Message}";
            }
        }

        private void RefreshFromService()
        {
            var config = _teachDataService.GetConfig();

            Groups.Clear();
            foreach (var g in config.Groups)
            {
                Groups.Add(new TeachGroupViewModel
                {
                    Name = g.Name,
                    Description = g.Description ?? "",
                    RequiredAxes = string.Join(", ", g.RequiredAxes)
                });
            }

            SelectedGroup = Groups.FirstOrDefault();
        }

        private void RefreshFilteredPositions()
        {
            FilteredPositions.Clear();
            if (SelectedGroup == null) return;

            var config = _teachDataService.GetConfig();
            var groupPositions = config.Positions
                .Where(p => p.Group.Equals(SelectedGroup.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var p in groupPositions)
            {
                var vm = new TeachPositionViewModel
                {
                    Name = p.Name,
                    Group = p.Group,
                    Description = p.Description ?? ""
                };
                foreach (var kvp in p.Axes)
                {
                    vm.Axes.Add(new AxisValueViewModel { AxisName = kvp.Key, Value = kvp.Value });
                }
                FilteredPositions.Add(vm);
            }

            SelectedPosition = FilteredPositions.FirstOrDefault();
        }

        private void ApplyChangesToService()
        {
            var config = new TeachDataConfig();

            // Groups
            foreach (var g in Groups)
            {
                config.Groups.Add(new TeachGroup
                {
                    Name = g.Name,
                    Description = string.IsNullOrEmpty(g.Description) ? null : g.Description,
                    RequiredAxes = g.RequiredAxes
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                });
            }

            // Positions: merge filtered (currently displayed) with positions from other groups
            var existingConfig = _teachDataService.GetConfig();
            var currentGroupName = SelectedGroup?.Name;

            // Add positions from non-selected groups (unchanged)
            foreach (var p in existingConfig.Positions)
            {
                if (currentGroupName != null &&
                    p.Group.Equals(currentGroupName, StringComparison.OrdinalIgnoreCase))
                    continue; // Skip current group — will add from FilteredPositions

                config.Positions.Add(p);
            }

            // Add positions from current group (from UI)
            foreach (var pVm in FilteredPositions)
            {
                var tp = new TeachPosition
                {
                    Name = pVm.Name,
                    Group = pVm.Group,
                    Description = string.IsNullOrEmpty(pVm.Description) ? null : pVm.Description
                };
                foreach (var axis in pVm.Axes)
                {
                    tp.Axes[axis.AxisName] = axis.Value;
                }
                config.Positions.Add(tp);
            }

            _teachDataService.LoadFrom(config, FilePath);
        }

        #endregion
    }

    #region Sub-ViewModels

    public partial class TeachGroupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _requiredAxes = string.Empty;

        public override string ToString() => Name;
    }

    public partial class TeachPositionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _group = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        public ObservableCollection<AxisValueViewModel> Axes { get; } = new();

        public string AxesSummary =>
            string.Join(", ", Axes.Select(a => $"{a.AxisName}={a.Value:F3}"));

        public override string ToString() => $"{Name} ({AxesSummary})";
    }

    public partial class AxisValueViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _axisName = string.Empty;

        [ObservableProperty]
        private double _value;
    }

    #endregion
}
