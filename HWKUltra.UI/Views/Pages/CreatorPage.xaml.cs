using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HWKUltra.UI.ViewModels.Controls;
using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class CreatorPage : Page, INavigableView<CreatorViewModel>
    {
        public CreatorViewModel ViewModel { get; }
        private readonly AIAssistantViewModel? _aiViewModel;

        public CreatorPage(CreatorViewModel viewModel, AIAssistantViewModel? aiViewModel = null)
        {
            ViewModel = viewModel;
            _aiViewModel = aiViewModel;
            DataContext = this;

            InitializeComponent();

            if (_aiViewModel != null)
            {
                // Provide a context snapshot to the AI panel: selected node + flow summary.
                _aiViewModel.ContextProvider = BuildAIContext;
                AIPanel.DataContext = _aiViewModel;
            }

            Loaded += CreatorPage_Loaded;
        }

        private string? BuildAIContext()
        {
            try
            {
                var selected = ViewModel.SelectedNode;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("You are an AOI flow design assistant. The user is editing a visual flow in the Creator page.");
                sb.AppendLine($"Current flow: {ViewModel.Nodes.Count} node(s), {ViewModel.Connections.Count} connection(s).");
                if (selected != null)
                {
                    sb.AppendLine($"Selected node: Type='{selected.NodeType}', Name='{selected.Name}'.");
                }
                sb.AppendLine("Answer concisely in Chinese when the user writes in Chinese, otherwise in English.");
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        private void CreatorPage_Loaded(object sender, RoutedEventArgs e)
        {
            // WPF-UI NavigationView wraps page content in an internal ScrollViewer
            // that gives pages infinite height. Walk up the visual tree to find it
            // and disable scrolling so this page is constrained to visible area.
            DependencyObject parent = this;
            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent is ScrollViewer scrollViewer)
                {
                    scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    break;
                }
            }
        }
    }
}
