using System.Windows.Input;
using HWKUltra.UI.Models;
using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class NodeTestPage : INavigableView<NodeTestViewModel>
    {
        public NodeTestViewModel ViewModel { get; }

        public NodeTestPage(NodeTestViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        /// <summary>
        /// Handle click on a node entry in the TreeView to select it
        /// </summary>
        private void NodeEntry_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement fe && fe.Tag is NodeCatalogEntry entry)
            {
                ViewModel.SelectedNodeEntry = entry;
            }
        }
    }
}
