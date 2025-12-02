using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.IO;
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
        private CancellationTokenSource? _filterDebounceCts;
        private readonly TimeSpan _filterDebounceDelay = TimeSpan.FromMilliseconds(100);
        private bool _isNavigatedTo = false;

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

        public ReadOnlyObservableCollection<Project> Projects { get; }

        public ProjectsViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider, IErrorDisplayService errorDisplayService)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _errorDisplayService = errorDisplayService;

            SelectedStatusFilter = "全部";

            Projects = _projectService.Projects;

            _projectService.ProjectPropertyChanged += OnProjectPropertyChanged;

            FilteredProjects = CollectionViewSource.GetDefaultView(Projects);
            if (FilteredProjects != null)
            {
                FilteredProjects.Filter = FilterProjects;
            }

            HasProjects = Projects.Any();
            if (Projects is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnProjectsCollectionChanged;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            DebouncedRefreshFilter();
        }

        partial void OnSelectedStatusFilterChanged(string? value)
        {
            // 状态过滤器立即响应，不需要防抖
            FilteredProjects?.Refresh();
        }

        /// <summary>
        /// 防抖刷新过滤器
        /// </summary>
        private void DebouncedRefreshFilter()
        {
            _filterDebounceCts?.Cancel();
            _filterDebounceCts = new CancellationTokenSource();
            var token = _filterDebounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_filterDebounceDelay, token);
                    if (!token.IsCancellationRequested)
                    {
                        Application.Current?.Dispatcher.Invoke(() => FilteredProjects?.Refresh());
                    }
                }
                catch (TaskCanceledException)
                {
                    // 忽略取消
                }
            }, token);
        }

        private bool FilterProjects(object obj)
        {
            if (obj is not Project project) return false;

            // 搜索文本筛选 - 使用OrdinalIgnoreCase替代ToLower以提高性能
            if (!string.IsNullOrEmpty(SearchText))
            {
                var hasMatch = project.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               project.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               project.Framework.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                if (!hasMatch)
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
        private async Task ManageComfyUIPlugins(Project project)
        {
            if (project == null) return;

            // 仅对 ComfyUI 项目启用
            if (string.IsNullOrWhiteSpace(project.Framework) ||
                !project.Framework.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase))
            {
                await _errorDisplayService.ShowWarningAsync("插件管理仅适用于 ComfyUI 类型的项目。", "不支持的项目类型");
                return;
            }

            // 计算 custom_nodes 目录：完全由 ComfyUI 根目录决定
            string? customNodesPath = null;

            var comfySettings = project.ComfyUISettings;
            if (comfySettings != null && !string.IsNullOrWhiteSpace(comfySettings.ComfyUIRootPath))
            {
                var root = comfySettings.ComfyUIRootPath;
                if (Directory.Exists(root))
                {
                    customNodesPath = Path.Combine(root, "custom_nodes");
                }
            }

            // 若未配置或根目录不存在，则提示错误，不再回退到项目本地路径
            if (string.IsNullOrWhiteSpace(customNodesPath))
            {
                await _errorDisplayService.ShowErrorAsync("未配置有效的 ComfyUI 根目录，无法定位 custom_nodes 目录。请在 ComfyUI 项目设置中指定根目录。", "路径错误");
                return;
            }

            // 如果目录不存在，先提示用户是否创建
            if (!Directory.Exists(customNodesPath))
            {
                var confirmed = await _errorDisplayService.ShowConfirmationAsync(
                    $"未找到 custom_nodes 目录，是否为项目创建？\n\n{customNodesPath}",
                    "创建插件目录");

                if (!confirmed)
                    return;

                try
                {
                    Directory.CreateDirectory(customNodesPath);
                }
                catch (Exception ex)
                {
                    await _errorDisplayService.ShowErrorAsync($"创建 custom_nodes 目录失败: {ex.Message}", "错误");
                    return;
                }
            }

            try
            {
                // 打开 ComfyUI 插件管理窗口
                var window = _serviceProvider.GetRequiredService<ProjectManager.Views.Dialogs.ComfyUIPluginsManagerWindow>();
                window.Initialize(customNodesPath, Application.Current.MainWindow);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                await _errorDisplayService.ShowErrorAsync($"打开插件管理窗口失败: {ex.Message}", "错误");
            }
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<NewProjectDialogViewModel>();
            var window = _serviceProvider.GetRequiredService<NewProjectWindow>();
            
            var result = window.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                await LoadProjects();
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
        private async Task ToggleProject(Project project)
        {
            if (project != null)
            {
                if (project.Status == ProjectStatus.Running || project.Status == ProjectStatus.Starting)
                {
                    await _projectService.StopProjectAsync(project);
                }
                else if (project.Status == ProjectStatus.Stopped || project.Status == ProjectStatus.Error)
                {
                    await _projectService.StartProjectAsync(project);
                }
            }
        }

        [RelayCommand]
        private async Task EditProject(Project project)
        {
            if (project != null)
            {
                var settingsWindowService = _serviceProvider.GetRequiredService<IProjectSettingsWindowService>();
                var result = settingsWindowService.ShowSettingsWindow(project, Application.Current.MainWindow);
                
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
                // 后台异步检查并清理无效的Git仓库（不阻塞UI）
                if (project.GitRepositories?.Count > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var gitService = _serviceProvider.GetRequiredService<IGitService>();
                            var projectService = _serviceProvider.GetRequiredService<IProjectService>();
                            
                            // 快速验证所有Git仓库的可用性
                            var validationResult = await gitService.ValidateRepositoriesAsync(project.GitRepositories);
                            
                            // 如果有无效的仓库，静默更新项目并保存
                            if (validationResult.InvalidRepositories.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"静默清理 {validationResult.InvalidRepositories.Count} 个无效的Git仓库");
                                
                                // 更新项目的Git仓库列表，移除无效的仓库
                                project.GitRepositories = validationResult.ValidRepositories;
                                project.LastModified = DateTime.Now;
                                
                                // 保存更新后的项目
                                await projectService.SaveProjectAsync(project);
                                
                                // 在UI线程上更新项目列表中的引用
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var existingProject = Projects.FirstOrDefault(p => p.Id == project.Id);
                                    if (existingProject != null)
                                    {
                                        existingProject.GitRepositories = project.GitRepositories;
                                        existingProject.LastModified = project.LastModified;
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"后台Git仓库清理失败: {ex.Message}");
                        }
                    });
                }

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

        [RelayCommand]
        private void OpenProjectInExplorer(Project project)
        {
            if (project?.LocalPath != null && Directory.Exists(project.LocalPath))
            {
                System.Diagnostics.Debug.WriteLine($"Opening project in Explorer: {project.LocalPath}");
                System.Diagnostics.Process.Start("explorer.exe", project.LocalPath);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Invalid project path or directory does not exist.");
            }
        }

        [RelayCommand]
        private void OpenProjectInVSCode(Project project)
        {
            if (project?.LocalPath != null && Directory.Exists(project.LocalPath))
            {
                try
                {
                    System.Diagnostics.Process.Start("code", $"\"{project.LocalPath}\"");
                }
                catch
                {
                    // 改为统一错误显示服务
                    _ = Task.Run(async () => await _errorDisplayService.ShowWarningAsync("无法启动 VS Code，请确保已安装 VS Code 并添加到系统路径", "错误"));
                }
            }
        }

        [RelayCommand]
        private async Task DeleteProject(Project project)
        {
            if (project == null) return;

            var confirmed = await _errorDisplayService.ShowConfirmationAsync(
                $"确定要删除项目 '{project.Name}' 吗？\n\n注意：这只会从项目管理器中移除项目记录，不会删除实际文件。",
                "确认删除");

            if (confirmed)
            {
                try
                {
                    await _projectService.DeleteProjectAsync(project.Id);
                    await LoadProjects();
                }
                catch (Exception ex)
                {
                    await _errorDisplayService.ShowErrorAsync($"删除项目失败: {ex.Message}");
                }
            }
        }

        private async Task LoadProjects()
        {
            try
            {
                await _projectService.GetProjectsAsync();
                HasProjects = Projects.Any();
                FilteredProjects?.Refresh();
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息
                System.Diagnostics.Debug.WriteLine($"加载项目失败: {ex.Message}");
            }
        }

        private void OnProjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasProjects = Projects.Any();
            FilteredProjects?.Refresh();
        }

        private void OnProjectPropertyChanged(object? sender, ProjectPropertyChangedEventArgs e)
        {
            // 只有在导航到此页面时才响应属性变化
            if (!_isNavigatedTo) return;
            
            if (e.PropertyName is nameof(Project.Name) or nameof(Project.Description) or nameof(Project.Framework) or nameof(Project.Status))
            {
                DebouncedRefreshFilter();
            }
        }

        public void OnNavigatedTo()
        {
            _isNavigatedTo = true;
            _ = LoadProjects();
        }

        public void OnNavigatedFrom()
        {
            _isNavigatedTo = false;
            _filterDebounceCts?.Cancel();
        }

        public async Task OnNavigatedToAsync()
        {
            _isNavigatedTo = true;
            await LoadProjects();
        }

        public async Task OnNavigatedFromAsync()
        {
            _isNavigatedTo = false;
            _filterDebounceCts?.Cancel();
            await Task.CompletedTask;
        }

        private async Task ShowErrorMessage(string message)
        {
            await _errorDisplayService.ShowErrorAsync(message, "错误");
        }
    }
}
