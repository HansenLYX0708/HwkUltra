using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class LoginPage : INavigableView<LoginViewModel>
    {
        public LoginViewModel ViewModel { get; }

        public LoginPage(LoginViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            // Focus username box on load
            Loaded += (_, _) => UsernameBox.Focus();
        }

        /// <summary>
        /// PasswordBox doesn't support direct binding — sync manually
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.PasswordBox pb)
            {
                ViewModel.Password = pb.Password;
            }
        }
    }
}
