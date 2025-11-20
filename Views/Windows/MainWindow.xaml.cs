using System.Windows;
using ProjectManager.ViewModels.Windows;
using ProjectManager.Services;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Windows.Controls;

namespace ProjectManager.Views.Windows
{
    public partial class MainWindow : FluentWindow, INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IContentDialogService contentDialogService,
            ISettingsService settingsService
        )
        {
            ViewModel = viewModel;
            _settingsService = settingsService;
            _navigationService = navigationService;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);
            
            // 设置ContentDialogService的宿主
            contentDialogService.SetDialogHost(RootContentDialog);

            // 窗口加载完成后设置为最大化
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 使用正确的方式最大化窗口
            this.WindowState = WindowState.Maximized;
            
            // 导航到默认启动页面
            await NavigateToDefaultStartupPage();
        }

        private async Task NavigateToDefaultStartupPage()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                var defaultPage = settings.DefaultStartupPage;

                Type? pageType = defaultPage switch
                {
                    "Dashboard" => typeof(Views.Pages.DashboardPage),
                    "Projects" => typeof(Views.Pages.ProjectsPage),
                    "Terminal" => typeof(Views.Pages.TerminalPage),
                    "Performance" => typeof(Views.Pages.PerformancePage),
                    _ => typeof(Views.Pages.DashboardPage) // 默认为仪表板
                };

                _navigationService.Navigate(pageType);
            }
            catch
            {
                // 如果出错，默认导航到仪表板
                _navigationService.Navigate(typeof(Views.Pages.DashboardPage));
            }
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

    public void SetServiceProvider(IServiceProvider serviceProvider) { }
    }
}
