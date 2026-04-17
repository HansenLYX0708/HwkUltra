using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class TeachDataPage : INavigableView<TeachDataViewModel>
    {
        public TeachDataViewModel ViewModel { get; }

        public TeachDataPage(TeachDataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
