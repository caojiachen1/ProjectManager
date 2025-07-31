using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManager.Views.Pages;
using ProjectManager.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ProjectManager.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        private INavigationWindow _navigationWindow = null!;

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
                _navigationWindow!.ShowWindow();

                // 配置 ContentDialogService 的 DialogHost
                var contentDialogService = _serviceProvider.GetService<IContentDialogService>();
                if (contentDialogService != null && _navigationWindow is MainWindow mainWindow)
                {
                    contentDialogService.SetDialogHost(mainWindow.RootContentDialog);
                }

                _navigationWindow.Navigate(typeof(Views.Pages.DashboardPage));
            }

            await Task.CompletedTask;
        }
    }
}
