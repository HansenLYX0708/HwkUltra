using HWKUltra.UI.Views.Pages;
using HWKUltra.UI.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace HWKUltra.UI.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        private INavigationWindow _navigationWindow;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _navigationWindow = (
                    _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow
                )!;

                // Apply persisted theme AFTER window construction (SystemThemeWatcher.Watch
                // is called in MainWindow constructor and overrides any prior theme apply)
                var settingsService = _serviceProvider.GetService(typeof(AppSettingsService)) as AppSettingsService;
                if (settingsService != null)
                {
                    var theme = settingsService.Settings.Theme;
                    ApplicationThemeManager.Apply(theme == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark);
                }

                _navigationWindow!.ShowWindow();

                _navigationWindow.Navigate(typeof(Views.Pages.LoginPage));
            }

            await Task.CompletedTask;
        }
    }
}
