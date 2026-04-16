using HWKUltra.UI.Services;

namespace HWKUltra.UI.ViewModels.Pages
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
        }

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isOfflineMode = true;

        [ObservableProperty]
        private bool _isLoggingIn;

        /// <summary>
        /// Raised when login succeeds — the host service will navigate away from login
        /// </summary>
        public event EventHandler? LoginSucceeded;

        [RelayCommand]
        private async Task Login()
        {
            ErrorMessage = string.Empty;
            IsLoggingIn = true;

            try
            {
                if (IsOfflineMode)
                {
                    var (success, message) = _authService.LoginOffline(Username, Password);
                    if (success)
                    {
                        LoginSucceeded?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ErrorMessage = message;
                    }
                }
                else
                {
                    var (success, message) = await _authService.LoginOnlineAsync(Username, Password);
                    if (success)
                    {
                        LoginSucceeded?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ErrorMessage = message;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsLoggingIn = false;
            }
        }
    }
}
