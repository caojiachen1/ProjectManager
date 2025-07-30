using System.Collections.ObjectModel;
using System.ComponentModel;
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
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace ProjectManager.ViewModels.Pages
{
    public partial class ProjectsViewModel : ObservableObject, INavigationAware
    {
        private readonly IProjectService _projectService;
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<AiProject> _projects = new();

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

        public ProjectsViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            
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

        partial void OnProjectsChanged(ObservableCollection<AiProject> value)
        {
            HasProjects = Projects.Any();
            FilteredProjects?.Refresh();
        }

        private bool FilterProjects(object obj)
        {
            if (obj is not AiProject project) return false;

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
                        var project = new AiProject
                        {
                            Name = Path.GetFileName(projectPath),
                            LocalPath = projectPath,
                            WorkingDirectory = projectPath,
                            CreatedDate = DateTime.Now,
                            LastModified = DateTime.Now
                        };

                        await _projectService.SaveProjectAsync(project);
                        await LoadProjects();
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("项目名称"))
                    {
                        await ShowErrorMessage(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorMessage($"导入项目失败: {ex.Message}");
                    }
                }
            }
        }

        [RelayCommand]
        private async Task StartProject(AiProject project)
        {
            if (project != null)
            {
                await _projectService.StartProjectAsync(project);
            }
        }

        [RelayCommand]
        private async Task StopProject(AiProject project)
        {
            if (project != null)
            {
                await _projectService.StopProjectAsync(project);
            }
        }

        [RelayCommand]
        private async Task EditProject(AiProject project)
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
        private void ViewLogs(AiProject project)
        {
            if (project != null)
            {
                // 获取日志页面的ViewModel
                var logsViewModel = _serviceProvider.GetService<ProjectLogsViewModel>();
                if (logsViewModel != null)
                {
                    // 加载项目到日志页面
                    logsViewModel.LoadProject(project.Id);
                    
                    // 导航到日志页面
                    _navigationService.Navigate(typeof(Views.Pages.ProjectLogsPage));
                }
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
            var messageBox = new MessageBox
            {
                Title = "错误",
                Content = message,
                PrimaryButtonText = "确定"
            };
            await messageBox.ShowDialogAsync();
        }
    }
}
