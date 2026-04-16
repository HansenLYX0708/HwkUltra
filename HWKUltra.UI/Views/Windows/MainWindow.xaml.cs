using HWKUltra.UI.Services;
using HWKUltra.UI.ViewModels.Pages;
using HWKUltra.UI.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace HWKUltra.UI.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }
        private readonly LoginViewModel _loginViewModel;
        private readonly AuthService _authService;

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            LoginViewModel loginViewModel,
            AuthService authService
        )
        {
            ViewModel = viewModel;
            _loginViewModel = loginViewModel;
            _authService = authService;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);

            // Disable navigation until authenticated
            RootNavigation.IsPaneVisible = false;
            RootNavigation.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;

            // Subscribe to login success
            _loginViewModel.LoginSucceeded += OnLoginSucceeded;

            // Subscribe to logout
            ViewModel.LogoutRequested += OnLogoutRequested;
        }

        private void OnLoginSucceeded(object? sender, EventArgs e)
        {
            // Update menu based on user role
            var role = _authService.CurrentSession?.Role ?? UserRole.None;
            ViewModel.UpdateMenuForRole(role);

            // Show navigation pane and controls
            RootNavigation.IsPaneVisible = true;
            RootNavigation.IsPaneToggleVisible = true;
            RootNavigation.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;

            // Navigate to dashboard
            RootNavigation.Navigate(typeof(Views.Pages.DashboardPage));
        }

        private void OnLogoutRequested(object? sender, EventArgs e)
        {
            // Hide navigation pane and controls
            RootNavigation.IsPaneVisible = false;
            RootNavigation.IsPaneToggleVisible = false;
            RootNavigation.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;

            // Navigate to login page
            RootNavigation.Navigate(typeof(Views.Pages.LoginPage));
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}
