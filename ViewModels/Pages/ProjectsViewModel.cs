using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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
    public partial class ProjectsViewModel : ObservableObject, INavigationAware
    {
        private readonly IProjectService _projectService;
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IErrorDisplayService _errorDisplayService;

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private ICollectionView? _filteredProjects;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string? _selectedStatusFilter;

        [ObservableProperty]
        private List<string> _statusFilters = new() { "全部", "运行中", "已停止", "错误" };

        [ObservableProperty]
        private bool _hasProjects = true;

        public ProjectsViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider, IErrorDisplayService errorDisplayService)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _errorDisplayService = errorDisplayService;

            SelectedStatusFilter = "全部";

            _projectService.ProjectStatusChanged += OnProjectStatusChanged;

            // 设置筛选
            FilteredProjects = CollectionViewSource.GetDefaultView(Projects);
            FilteredProjects.Filter = FilterProjects;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredProjects?.Refresh();
        }

        partial void OnSelectedStatusFilterChanged(string? value)
        {
            FilteredProjects?.Refresh();
        }

        partial void OnProjectsChanged(ObservableCollection<Project> value)
        {
            HasProjects = Projects.Any();
            FilteredProjects?.Refresh();
        }

        private bool FilterProjects(object obj)
        {
            if (obj is not Project project) return false;

            // 搜索文本筛选
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!project.Name.ToLower().Contains(searchLower) &&
                    !project.Description.ToLower().Contains(searchLower) &&
                    !project.Framework.ToLower().Contains(searchLower))
                {
                    return false;
                }
            }

            // 状态筛选
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "全部")
            {
                var statusMatch = SelectedStatusFilter switch
                {
                    "运行中" => project.Status == ProjectStatus.Running,
                    "已停止" => project.Status == ProjectStatus.Stopped,
                    "错误" => project.Status == ProjectStatus.Error,
                    _ => true
                };
                if (!statusMatch) return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<ProjectEditDialogViewModel>();
            
            // 加载空项目（新建模式）
            dialogViewModel.LoadProject();
            
            // 创建窗口并手动设置 DataContext
            var window = new Views.Dialogs.ProjectEditWindow(dialogViewModel);
            
            var result = window.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                await LoadProjects();
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
                    try
                    {
                        var project = new Project
                        {
                            Name = Path.GetFileName(projectPath),
                            LocalPath = projectPath,
                            WorkingDirectory = projectPath,
                            CreatedDate = DateTime.Now,
                            LastModified = DateTime.Now
                        };

                        var saveSuccess = await _projectService.SaveProjectAsync(project);
                        if (saveSuccess)
                        {
                            await LoadProjects();
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorMessage($"导入项目失败: {ex.Message}");
                    }
                }
            }
        }

        [RelayCommand]
        private async Task CloneFromGit()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<GitCloneDialogViewModel>();
            var window = _serviceProvider.GetRequiredService<Views.Dialogs.GitCloneWindow>();
            
            window.Owner = Application.Current.MainWindow;
            var result = window.ShowDialog();
            
            if (result == true)
            {
                // 克隆成功，刷新项目列表
                await LoadProjects();
            }
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

        [RelayCommand]
        private async Task EditProject(Project project)
        {
            if (project != null)
            {
                var dialogViewModel = _serviceProvider.GetRequiredService<ProjectEditDialogViewModel>();
                
                // 先加载项目数据
                dialogViewModel.LoadProject(project);
                
                // 创建窗口并手动设置 DataContext
                var window = new Views.Dialogs.ProjectEditWindow(dialogViewModel);
                
                var result = window.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    await LoadProjects();
                }
            }
        }

        [RelayCommand]
        private async Task ManageEnvironmentVariables(Project project)
        {
            if (project != null)
            {
                var dialogViewModel = _serviceProvider.GetRequiredService<EnvironmentVariablesDialogViewModel>();
                
                // 加载项目数据
                dialogViewModel.LoadProject(project);
                
                // 创建窗口
                var window = new Views.Dialogs.EnvironmentVariablesWindow(dialogViewModel);
                window.Owner = Application.Current.MainWindow;
                
                var result = window.ShowDialog();
                if (result == true)
                {
                    // 保存项目更改
                    var saveSuccess = await _projectService.SaveProjectAsync(project);
                    if (saveSuccess)
                    {
                        await LoadProjects();
                    }
                }
            }
        }

        [RelayCommand]
        private async Task ManageGit(Project project)
        {
            if (project != null)
            {
                var dialogViewModel = _serviceProvider.GetRequiredService<GitManagementDialogViewModel>();
                var dialog = new GitManagementWindow();
                
                // 设置ViewModel
                dialog.SetViewModel(dialogViewModel);
                
                // 订阅Git信息更新事件
                dialogViewModel.GitInfoUpdated += (sender, updatedProject) =>
                {
                    // 更新项目列表中的Git信息
                    var existingProject = Projects.FirstOrDefault(p => p.Id == updatedProject.Id);
                    if (existingProject != null)
                    {
                        existingProject.GitInfo = updatedProject.GitInfo;
                    }
                };
                
                await dialogViewModel.LoadProjectAsync(project);
                
                // 设置所有者窗口并显示为模态对话框
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
        }

        [RelayCommand]
        private void ViewLogs(Project project)
        {
            if (project != null)
            {
                // 获取终端页面的ViewModel并设置项目路径，但不自动启动
                var terminalViewModel = _serviceProvider.GetService<TerminalViewModel>();
                if (terminalViewModel != null)
                {
                    // 只设置项目路径，不设置启动命令，避免自动启动
                    terminalViewModel.SetProjectPath(project.Name, project.LocalPath);
                }
                
                // 直接导航到终端页面
                _navigationService.Navigate(typeof(Views.Pages.TerminalPage));
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadProjects();
        }

        private async Task LoadProjects()
        {
            try
            {
                var projects = await _projectService.GetProjectsAsync();
                Projects.Clear();
                foreach (var project in projects)
                {
                    Projects.Add(project);
                }
                HasProjects = Projects.Any();
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息
                System.Diagnostics.Debug.WriteLine($"加载项目失败: {ex.Message}");
            }
        }

        private void OnProjectStatusChanged(object? sender, ProjectStatusChangedEventArgs e)
        {
            var project = Projects.FirstOrDefault(p => p.Id == e.ProjectId);
            if (project != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    project.Status = e.Status;
                });
            }
        }

        public void OnNavigatedTo()
        {
            _ = LoadProjects();
        }

        public void OnNavigatedFrom() { }

        public async Task OnNavigatedToAsync()
        {
            await LoadProjects();
        }

        public async Task OnNavigatedFromAsync()
        {
            await Task.CompletedTask;
        }

        private async Task ShowErrorMessage(string message)
        {
            await _errorDisplayService.ShowErrorAsync(message, "错误");
        }
    }
}
