using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HWKUltra.UI.Services;
using Wpf.Ui.Controls;

namespace HWKUltra.UI.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        public MainWindowViewModel(AuthService authService)
        {
            _authService = authService;
            _authService.AuthStateChanged += OnAuthStateChanged;
        }

        [ObservableProperty]
        private string _applicationTitle = "Hawkeye Ultra";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new();

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new();

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Dashboard", Tag = "tray_home" }
        };

        [ObservableProperty]
        private bool _isNavigationEnabled;

        [ObservableProperty]
        private string _currentUser = string.Empty;

        /// <summary>
        /// Raised when user requests logout — MainWindow handles nav reset
        /// </summary>
        public event EventHandler? LogoutRequested;

        /// <summary>
        /// Rebuild navigation menu based on current user role
        /// </summary>
        public void UpdateMenuForRole(UserRole role)
        {
            MenuItems.Clear();
            FooterMenuItems.Clear();

            if (role == UserRole.None)
            {
                IsNavigationEnabled = false;
                return;
            }

            IsNavigationEnabled = true;

            // Common items — visible to all authenticated users
            MenuItems.Add(new NavigationViewItem()
            {
                Content = "Dashboard",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            });

            MenuItems.Add(new NavigationViewItem()
            {
                Content = "Creator",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Diagram24 },
                TargetPageType = typeof(Views.Pages.CreatorPage)
            });

            // Admin-only items
            if (role == UserRole.Admin)
            {
                MenuItems.Add(new NavigationViewItem()
                {
                    Content = "Node Test",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 },
                    TargetPageType = typeof(Views.Pages.NodeTestPage)
                });

                MenuItems.Add(new NavigationViewItem()
                {
                    Content = "Device Config",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.WrenchScrewdriver24 },
                    TargetPageType = typeof(Views.Pages.DeviceConfigPage)
                });

                MenuItems.Add(new NavigationViewItem()
                {
                    Content = "Teach Data",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Location24 },
                    TargetPageType = typeof(Views.Pages.TeachDataPage)
                });
            }

            // Footer items — always visible when logged in
            FooterMenuItems.Add(new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            });

            // Update current user display
            CurrentUser = _authService.CurrentSession?.Username ?? "";
        }

        [RelayCommand]
        private void Logout()
        {
            _authService.Logout();
            CurrentUser = string.Empty;
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnAuthStateChanged(object? sender, UserSession? session)
        {
            var role = session?.Role ?? UserRole.None;
            System.Windows.Application.Current.Dispatcher.Invoke(() => UpdateMenuForRole(role));
        }
    }
}
