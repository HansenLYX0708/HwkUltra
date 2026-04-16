using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class DeviceConfigPage : INavigableView<DeviceConfigViewModel>
    {
        public DeviceConfigViewModel ViewModel { get; }

        public DeviceConfigPage(DeviceConfigViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
