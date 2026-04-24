using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using HWKUltra.UI.Services;
using HWKUltra.UI.ViewModels.Pages;

namespace HWKUltra.UI.Views.Windows
{
    /// <summary>
    /// Standalone window that hosts an independent FlowCanvas for viewing a sub-flow.
    /// Reuses CreatorViewModel so all canvas features (pan, zoom, selection, sub-flow pop-out) work.
    /// The user may open as many windows as they want, each showing a different flow file.
    /// </summary>
    public partial class FlowViewerWindow : Window, INotifyPropertyChanged
    {
        public CreatorViewModel ViewModel { get; }

        // Owned by this window and shared with the ViewModel so that
        // CurrentFilePath is visible to sub-flow pop-out logic inside the ViewModel.
        private readonly FlowDocumentService _localDocService;

        private string? _openFilePath;
        private string _breadcrumbText = string.Empty;

        /// <summary>
        /// Joined breadcrumb for display (e.g. "Main.json &gt; Stage1_Test.json &gt; TrayTest.json").
        /// </summary>
        public string BreadcrumbText
        {
            get => _breadcrumbText;
            private set { _breadcrumbText = value; Notify(); }
        }

        public FlowViewerWindow(
            FlowDocumentService documentService,
            NodeCatalogService catalogService,
            AppSettingsService settingsService)
        {
            // Each window gets its own FlowDocumentService so CurrentFilePath does
            // not clash with the main editor or other viewer windows. The same
            // instance is handed to the ViewModel so loading via OpenFile() below
            // updates a path the ViewModel can read (for relative sub-flow resolution).
            _localDocService = new FlowDocumentService();
            ViewModel = new CreatorViewModel(_localDocService, catalogService, settingsService);

            InitializeComponent();

            // CreatorViewModel lazily initializes on OnNavigatedToAsync; we aren't a Page
            // so manually ensure categories are loaded and a blank definition exists.
            _ = ((Wpf.Ui.Abstractions.Controls.INavigationAware)ViewModel).OnNavigatedToAsync();

            // FlowCanvas casts DataContext as CreatorViewModel internally.
            DataContext = ViewModel;
            FlowCanvasControl.DataContext = ViewModel;
        }

        /// <summary>
        /// Load the given flow file into this window and append to the breadcrumb.
        /// </summary>
        public void OpenFile(string fullPath, System.Collections.Generic.List<string> parentBreadcrumb)
        {
            _openFilePath = fullPath;
            ViewModel.Breadcrumb = new System.Collections.Generic.List<string>(parentBreadcrumb)
            {
                Path.GetFileName(fullPath)
            };
            BreadcrumbText = string.Join("  >  ", ViewModel.Breadcrumb);

            // Load via the ViewModel's own document service so CurrentFilePath is
            // populated — nested sub-flow pop-outs rely on it to resolve relative paths.
            var def = _localDocService.LoadFromFile(fullPath);
            if (def != null)
            {
                ViewModel.LoadFromDefinition(def);
                ViewModel.FlowName = def.Name;
                ViewModel.StatusText = $"Loaded {Path.GetFileName(fullPath)}";
            }
            else
            {
                ViewModel.StatusText = $"Failed to load: {fullPath}";
            }

            Title = $"Flow Viewer - {BreadcrumbText}";
        }

        private void OpenInEditor_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_openFilePath) || !File.Exists(_openFilePath))
                return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _openFilePath,
                    UseShellExecute = true
                });
            }
            catch { /* user can open manually */ }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
