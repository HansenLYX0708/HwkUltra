using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HWKUltra.Creator.Services;
using HWKUltra.Creator.ViewModels;

namespace HWKUltra.Creator
{
    public partial class App : Application
    {
        private static readonly IHost _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<NodeCatalogService>();
                services.AddSingleton<FlowDocumentService>();
                services.AddSingleton<EditorConfigService>();

                // ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<FlowCanvasViewModel>();
                services.AddTransient<NodeToolboxViewModel>();
                services.AddTransient<PropertyPanelViewModel>();

                // Windows
                services.AddSingleton<MainWindow>();
            })
            .Build();

        public static IServiceProvider Services => _host.Services;

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
