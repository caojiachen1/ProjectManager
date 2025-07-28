using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;

namespace ProjectManager.ViewModels.Pages
{
    public partial class ProjectLogsViewModel : ObservableObject, INavigationAware
    {
        private readonly IProjectService _projectService;
        private readonly DispatcherTimer _refreshTimer;
        private AiProject? _currentProject;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private ProjectStatus _projectStatus = ProjectStatus.Stopped;

        [ObservableProperty]
        private string _logContent = string.Empty;

        [ObservableProperty]
        private bool _autoScroll = true;

        [ObservableProperty]
        private string _statusText = "就绪";

        [ObservableProperty]
        private int _logLineCount = 0;

        [ObservableProperty]
        private DateTime _lastUpdated = DateTime.Now;

        public ProjectLogsViewModel(IProjectService projectService)
        {
            _projectService = projectService;
            
            // 设置定时器每秒刷新日志
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshLogs();
        }

        public void LoadProject(string projectId)
        {
            Task.Run(async () =>
            {
                _currentProject = await _projectService.GetProjectAsync(projectId);
                if (_currentProject != null)
                {
                    ProjectName = _currentProject.Name;
                    ProjectStatus = _currentProject.Status;
                    await RefreshLogs();
                    
                    // 如果项目正在运行，启动自动刷新
                    if (_currentProject.Status == ProjectStatus.Running)
                    {
                        _refreshTimer.Start();
                    }
                }
            });
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await RefreshLogs();
        }

        [RelayCommand]
        private async Task ClearLogs()
        {
            if (_currentProject != null)
            {
                _currentProject.LogOutput = string.Empty;
                await _projectService.SaveProjectAsync(_currentProject);
                LogContent = string.Empty;
                LogLineCount = 0;
                StatusText = "日志已清空";
                LastUpdated = DateTime.Now;
            }
        }

        [RelayCommand]
        private async Task ExportLogs()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "导出日志",
                    Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    FileName = $"{ProjectName}_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllTextAsync(dialog.FileName, LogContent);
                    StatusText = $"日志已导出到: {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"导出失败: {ex.Message}";
            }
        }

        private async Task RefreshLogs()
        {
            try
            {
                if (_currentProject != null)
                {
                    var updatedProject = await _projectService.GetProjectAsync(_currentProject.Id);
                    if (updatedProject != null)
                    {
                        _currentProject = updatedProject;
                        ProjectStatus = updatedProject.Status;
                        
                        var newLogContent = updatedProject.LogOutput ?? string.Empty;
                        if (newLogContent != LogContent)
                        {
                            LogContent = newLogContent;
                            LogLineCount = LogContent.Split('\n', StringSplitOptions.None).Length;
                            LastUpdated = DateTime.Now;
                        }

                        // 更新状态文本
                        StatusText = ProjectStatus switch
                        {
                            ProjectStatus.Running => "项目运行中...",
                            ProjectStatus.Stopped => "项目已停止",
                            ProjectStatus.Starting => "项目启动中...",
                            ProjectStatus.Stopping => "项目停止中...",
                            ProjectStatus.Error => "项目运行出错",
                            _ => "未知状态"
                        };

                        // 如果项目停止了，停止自动刷新
                        if (ProjectStatus != ProjectStatus.Running && _refreshTimer.IsEnabled)
                        {
                            _refreshTimer.Stop();
                        }
                        // 如果项目开始运行，启动自动刷新
                        else if (ProjectStatus == ProjectStatus.Running && !_refreshTimer.IsEnabled)
                        {
                            _refreshTimer.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"刷新失败: {ex.Message}";
            }
        }

        public void OnNavigatedTo()
        {
            // 在导航到页面时会调用LoadProject
        }

        public void OnNavigatedFrom()
        {
            _refreshTimer.Stop();
        }

        public async Task OnNavigatedToAsync()
        {
            await Task.CompletedTask;
        }

        public async Task OnNavigatedFromAsync()
        {
            _refreshTimer.Stop();
            await Task.CompletedTask;
        }
    }
}
