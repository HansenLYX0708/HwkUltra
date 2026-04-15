using Wpf.Ui.Controls;
using HWKUltra.Creator.ViewModels;

namespace HWKUltra.Creator
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
