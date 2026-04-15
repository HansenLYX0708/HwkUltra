using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class CreatorPage : Page, INavigableView<CreatorViewModel>
    {
        public CreatorViewModel ViewModel { get; }

        public CreatorPage(CreatorViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            Loaded += CreatorPage_Loaded;
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
