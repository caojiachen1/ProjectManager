using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Models;
using ProjectManager.Services;
using ProjectManager.ViewModels.Dialogs;
using ProjectManager.Views.Dialogs;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectManager.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        private readonly IProjectService _projectService;
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IErrorDisplayService _errorDisplayService;
        private CancellationTokenSource? _debounceCts;
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(150);
        private bool _isNavigatedTo = false;

        [ObservableProperty]
        private int _totalProjects = 0;

        [ObservableProperty]
        private int _runningProjects = 0;

        [ObservableProperty]
        private int _stoppedProjects = 0;

        [ObservableProperty]
        private int _errorProjects = 0;

        [ObservableProperty]
        private ObservableCollection<Project> _recentProjects = new();

        public DashboardViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider, IErrorDisplayService errorDisplayService)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _errorDisplayService = errorDisplayService;
            
            _projectService.ProjectPropertyChanged += OnProjectPropertyChanged;
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<ProjectEditDialogViewModel>();
            var window = _serviceProvider.GetRequiredService<Views.Dialogs.ProjectEditWindow>();
            
            dialogViewModel.LoadProject();
            
            var result = window.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                await LoadDashboardData();
            }
        }

        [RelayCommand]
        private async Task GitClone()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<GitCloneDialogViewModel>();
            var window = _serviceProvider.GetRequiredService<Views.Dialogs.GitCloneWindow>();
            
            dialogViewModel.CloneCompleted += async (sender, result) =>
            {
                if (result)
                {
                    await LoadDashboardData();
                }
            };
            
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void ViewAllProjects()
        {
            _navigationService.Navigate(typeof(Views.Pages.ProjectsPage));
        }

        [RelayCommand]
        private async Task StopAllProjects()
        {
            var projects = await _projectService.GetProjectsAsync();
            var runningProjects = projects.Where(p => p.Status == ProjectStatus.Running);
            
            foreach (var project in runningProjects)
            {
                await _projectService.StopProjectAsync(project);
            }
            
            await LoadDashboardData();
        }

        [RelayCommand]
        private async Task StartProject(Project project)
        {
            if (project != null)
            {
                await _projectService.StartProjectAsync(project);
            }
        }

        [RelayCommand]
        private async Task StopProject(Project project)
        {
            if (project != null)
            {
                await _projectService.StopProjectAsync(project);
            }
        }

        private async Task LoadDashboardData()
        {
            try
            {
                await _projectService.GetProjectsAsync();
                RecalculateDashboard();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载Dashboard数据失败: {ex.Message}");
                // 显示关键错误给用户
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"加载仪表板数据失败: {ex.Message}", "数据加载错误"));
            }
        }

        private void OnProjectPropertyChanged(object? sender, ProjectPropertyChangedEventArgs e)
        {
            // 只有在导航到此页面时才响应属性变化
            if (!_isNavigatedTo) return;
            
            if (e.PropertyName is nameof(Project.Status) or nameof(Project.LastModified))
            {
                // 使用防抖减少频繁刷新
                DebouncedRecalculateDashboard();
            }
        }

        private void DebouncedRecalculateDashboard()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_debounceDelay, token);
                    if (!token.IsCancellationRequested)
                    {
                        Application.Current?.Dispatcher.Invoke(RecalculateDashboard);
                    }
                }
                catch (TaskCanceledException)
                {
                    // 忽略取消
                }
            }, token);
        }

        private void RecalculateDashboard()
        {
            var projects = _projectService.Projects;

            TotalProjects = projects.Count;
            RunningProjects = projects.Count(p => p.Status == ProjectStatus.Running);
            StoppedProjects = projects.Count(p => p.Status == ProjectStatus.Stopped);
            ErrorProjects = projects.Count(p => p.Status == ProjectStatus.Error);

            var recent = projects
                .OrderByDescending(p => p.LastModified)
                .Take(5)
                .ToList();

            RecentProjects.Clear();
            foreach (var project in recent)
            {
                RecentProjects.Add(project);
            }
        }

        public void OnNavigatedTo()
        {
            _isNavigatedTo = true;
            _ = LoadDashboardData();
        }

        public void OnNavigatedFrom()
        {
            _isNavigatedTo = false;
            _debounceCts?.Cancel();
        }

        public async Task OnNavigatedToAsync()
        {
            _isNavigatedTo = true;
            await LoadDashboardData();
        }

        public async Task OnNavigatedFromAsync()
        {
            _isNavigatedTo = false;
            _debounceCts?.Cancel();
            await Task.CompletedTask;
        }
    }
}
