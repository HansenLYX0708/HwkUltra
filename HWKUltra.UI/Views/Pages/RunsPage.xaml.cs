using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class RunsPage : INavigableView<RunsViewModel>
    {
        public RunsViewModel ViewModel { get; }

        public RunsPage(RunsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
