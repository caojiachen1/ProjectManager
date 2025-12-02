﻿using System.IO;
using System.Reflection;
using System.Windows.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManager.Services;
using ProjectManager.ViewModels.Pages;
using ProjectManager.ViewModels.Windows;
using ProjectManager.Views.Pages;
using ProjectManager.Views.Windows;
using ProjectManager.Helpers;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace ProjectManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // 使用Lazy延迟初始化Host以加快应用启动
        private static readonly Lazy<IHost> _hostLazy = new Lazy<IHost>(() => CreateHost(), LazyThreadSafetyMode.ExecutionAndPublication);
        private static IHost _host => _hostLazy.Value;

        private static IHost CreateHost() => Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty); })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Content Dialog Service
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // Project service
                services.AddSingleton<IProjectService, ProjectService>();
                
                // Git service
                services.AddSingleton<IGitService, GitService>();
                
                // Error display service
                services.AddSingleton<IErrorDisplayService, ErrorDisplayService>();

                // Performance monitor service
                services.AddSingleton<IPerformanceMonitorService, PerformanceMonitorService>();

                // Settings service
                services.AddSingleton<ISettingsService, SettingsService>();

                // Terminal service
                // Terminal service
                services.AddSingleton<TerminalService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // Pages and ViewModels
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ProjectsPage>();
                services.AddSingleton<ProjectsViewModel>();
                services.AddSingleton<AddProjectPage>();
                services.AddSingleton<AddProjectViewModel>();
                services.AddSingleton<PerformancePage>();
                services.AddSingleton<PerformanceViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<TerminalPage>();
                services.AddSingleton<TerminalViewModel>();
                
                // System Environment Variables page
                services.AddSingleton<SystemEnvironmentVariablesPage>();
                services.AddSingleton<SystemEnvironmentVariablesViewModel>();
                
                // Project Settings Window Service
                services.AddTransient<IProjectSettingsWindowService, ProjectSettingsWindowService>();
                
                // Dialogs
                services.AddTransient<Views.Dialogs.NewProjectWindow>();
                services.AddTransient<ViewModels.Dialogs.NewProjectDialogViewModel>();
                
                // Framework-specific settings windows
                services.AddTransient<ViewModels.Dialogs.ComfyUIProjectSettingsViewModel>();
                services.AddTransient<ViewModels.Dialogs.NodeJSProjectSettingsViewModel>();
                services.AddTransient<ViewModels.Dialogs.DotNetProjectSettingsViewModel>();
                services.AddTransient<ViewModels.Dialogs.GenericProjectSettingsViewModel>();
                
                // Keep original dialogs for compatibility
                services.AddTransient<Views.Dialogs.ProjectEditWindow>();
                services.AddTransient<ViewModels.Dialogs.ProjectEditDialogViewModel>();
                services.AddTransient<Views.Dialogs.GitManagementWindow>();
                services.AddTransient<ViewModels.Dialogs.GitManagementDialogViewModel>();
                services.AddTransient<Views.Dialogs.GitCloneWindow>();
                services.AddTransient<ViewModels.Dialogs.GitCloneDialogViewModel>();
                services.AddTransient<Views.Dialogs.EnvironmentVariablesWindow>();
                services.AddTransient<ViewModels.Dialogs.EnvironmentVariablesDialogViewModel>();

                // ComfyUI 插件管理窗口
                services.AddTransient<Views.Dialogs.ComfyUIPluginsManagerWindow>();
                services.AddTransient<ViewModels.Dialogs.ComfyUIPluginsManagerViewModel>();
                
                // 编辑环境变量对话框
                services.AddTransient<Views.Dialogs.EditEnvironmentVariableWindow>();
                services.AddTransient<ViewModels.Dialogs.EditEnvironmentVariableViewModel>();
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services => _host.Services;

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            // Suppress known WPF DataGrid binding errors (CellsPanelHorizontalOffset issue)
            DataGridBindingErrorSuppressor.Initialize();
            
            await _host.StartAsync();

            // 注册全局未处理异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            try
            {
                // 确保在应用退出时终止所有后台运行的项目进程
                var terminalService = Services.GetService(typeof(TerminalService)) as TerminalService;
                terminalService?.Cleanup();
            }
            catch { /* 退出阶段忽略清理异常 */ }

            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var errorService = Services.GetService(typeof(IErrorDisplayService)) as IErrorDisplayService;
                if (errorService != null)
                {
                    _ = errorService.ShowExceptionAsync(e.Exception, "未处理UI异常");
                }
            }
            catch { /* 忽略二次异常 */ }
            // 默认不终止，让应用决定后续行为
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var errorService = Services.GetService(typeof(IErrorDisplayService)) as IErrorDisplayService;
                if (errorService != null && e.ExceptionObject is Exception ex)
                {
                    _ = errorService.ShowExceptionAsync(ex, "未处理系统异常");
                }
            }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var errorService = Services.GetService(typeof(IErrorDisplayService)) as IErrorDisplayService;
                if (errorService != null)
                {
                    _ = errorService.ShowExceptionAsync(e.Exception, "未观察任务异常");
                }
            }
            catch { }
            finally
            {
                e.SetObserved();
            }
        }
    }
}
