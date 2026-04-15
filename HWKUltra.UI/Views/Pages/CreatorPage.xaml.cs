using System.Windows.Controls;
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
        }
    }
}
