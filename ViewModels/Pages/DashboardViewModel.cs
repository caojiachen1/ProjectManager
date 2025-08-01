using System.Collections.ObjectModel;
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

        public DashboardViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            
            _projectService.ProjectStatusChanged += OnProjectStatusChanged;
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
        private async Task ImportProject()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择项目文件夹",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                var projectPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(projectPath))
                {
                    var project = new Project
                    {
                        Name = Path.GetFileName(projectPath),
                        LocalPath = projectPath,
                        WorkingDirectory = projectPath,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    };

                    await _projectService.SaveProjectAsync(project);
                    await LoadDashboardData();
                }
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
                var projects = await _projectService.GetProjectsAsync();
                
                TotalProjects = projects.Count;
                RunningProjects = projects.Count(p => p.Status == ProjectStatus.Running);
                StoppedProjects = projects.Count(p => p.Status == ProjectStatus.Stopped);
                ErrorProjects = projects.Count(p => p.Status == ProjectStatus.Error);

                // 获取最近的5个项目
                var recentProjectsList = projects
                    .OrderByDescending(p => p.LastModified)
                    .Take(5)
                    .ToList();

                RecentProjects.Clear();
                foreach (var project in recentProjectsList)
                {
                    RecentProjects.Add(project);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载Dashboard数据失败: {ex.Message}");
            }
        }

        private void OnProjectStatusChanged(object? sender, ProjectStatusChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadDashboardData();
            });
        }

        public void OnNavigatedTo()
        {
            _ = LoadDashboardData();
        }

        public void OnNavigatedFrom() { }

        public async Task OnNavigatedToAsync()
        {
            await LoadDashboardData();
        }

        public async Task OnNavigatedFromAsync()
        {
            await Task.CompletedTask;
        }
    }
}
