﻿using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManager.Services;
using ProjectManager.ViewModels.Pages;
using ProjectManager.ViewModels.Windows;
using ProjectManager.Views.Pages;
using ProjectManager.Views.Windows;
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
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
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
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<TerminalPage>();
                services.AddSingleton<TerminalViewModel>();
                
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
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
