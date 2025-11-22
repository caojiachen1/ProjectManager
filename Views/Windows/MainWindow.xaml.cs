using System.Windows;
using System.ComponentModel;
using System.Linq;
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
        /// 在窗口尝试关闭时检查是否有正在运行的项目，若有则提示用户确认。
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                // 使用 Fluent 风格的对话框（通过 IErrorDisplayService），保持应用内一致性
                var terminalService = App.Services.GetService(typeof(Services.TerminalService)) as Services.TerminalService;
                if (terminalService != null)
                {
                    var sessions = terminalService.GetAllSessions();
                    if (sessions != null && sessions.Any(s => s.IsRunning))
                    {
                        var errorService = App.Services.GetService(typeof(Services.IErrorDisplayService)) as Services.IErrorDisplayService;
                        bool confirm = false;
                        if (errorService != null)
                        {
                            // ShowConfirmationAsync 返回 bool，阻塞等待结果以在同步 OnClosing 中使用
                            confirm = errorService.ShowConfirmationAsync("仍有项目在运行，确认关闭将会终止这些运行中的项目并退出程序。是否仍然关闭？", "确认退出").GetAwaiter().GetResult();
                        }
                        else
                        {
                            // 回退到系统对话框（不太理想，但保证行为一致）
                            confirm = System.Windows.MessageBox.Show("仍有项目在运行，确认关闭将会终止这些运行中的项目并退出程序。是否仍然关闭？",
                                "确认退出", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes;
                        }

                        if (confirm)
                        {
                            // 停止并清理所有正在运行的会话，然后允许窗口关闭
                            try
                            {
                                foreach (var s in sessions.Where(s => s.IsRunning))
                                {
                                    try { terminalService.StopSession(s); } catch { }
                                }
                                terminalService.Cleanup();
                            }
                            catch { /* 忽略清理中的异常，继续退出 */ }
                        }
                        else
                        {
                            // 取消关闭
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }
            catch { /* 忽略检查过程中的异常，继续关闭以避免阻塞退出 */ }

            base.OnClosing(e);
        }

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
