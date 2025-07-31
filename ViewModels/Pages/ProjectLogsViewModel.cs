using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectManager.ViewModels.Pages
{
    public partial class ProjectLogsViewModel : ObservableObject, INavigationAware
    {
        private readonly IProjectService _projectService;
        private readonly INavigationService _navigationService;
        private readonly TerminalService _terminalService;
        private readonly DispatcherTimer _refreshTimer;
        private Project? _currentProject;

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

        public ProjectLogsViewModel(IProjectService projectService, INavigationService navigationService, TerminalService terminalService)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _terminalService = terminalService;
            
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

        [RelayCommand]
        private async Task GoToTerminal()
        {
            try
            {
                StatusText = "正在跳转到终端页面...";
                
                // 导航到终端页面
                _navigationService.Navigate(typeof(Views.Pages.TerminalPage));
                
                // 延迟一下确保页面加载完成
                await Task.Delay(500);
                
                // 如果当前项目存在，尝试切换到对应的终端会话
                if (_currentProject != null)
                {
                    // 获取终端页面的ViewModel并切换到对应项目的终端
                    var terminalViewModel = App.Services.GetService(typeof(TerminalViewModel)) as TerminalViewModel;
                    if (terminalViewModel != null)
                    {
                        // 检查是否已存在该项目的终端会话
                        var existingSession = terminalViewModel.TerminalSessions.FirstOrDefault(s => s.ProjectName == _currentProject.Name);
                        if (existingSession != null)
                        {
                            // 切换到现有会话
                            terminalViewModel.SwitchToProjectTerminal(_currentProject.Name);
                            StatusText = $"已切换到 {_currentProject.Name} 的终端会话";
                        }
                        else
                        {
                            // 创建新的终端会话
                            var startCommand = !string.IsNullOrEmpty(_currentProject.StartCommand) 
                                ? _currentProject.StartCommand 
                                : "dir";
                            
                            await terminalViewModel.CreateAndStartSessionAsync(
                                _currentProject.Name, 
                                _currentProject.LocalPath, 
                                startCommand);
                            
                            StatusText = $"已为 {_currentProject.Name} 创建新的终端会话";
                        }
                    }
                    else
                    {
                        StatusText = "终端服务未找到";
                    }
                }
                else
                {
                    StatusText = "已跳转到终端页面";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"跳转失败: {ex.Message}";
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
                        
                        // 先从项目自身获取日志
                        var projectLogContent = updatedProject.LogOutput ?? string.Empty;
                        
                        // 如果有对应的终端会话，也获取终端日志
                        var terminalSessions = _terminalService.GetAllSessions();
                        var relatedSession = terminalSessions.FirstOrDefault(s => s.ProjectName == _currentProject.Name);
                        
                        string combinedLogContent = projectLogContent;
                        if (relatedSession != null && relatedSession.OutputLines.Any())
                        {
                            var terminalOutput = string.Join("\n", relatedSession.OutputLines);
                            if (!string.IsNullOrEmpty(terminalOutput))
                            {
                                combinedLogContent += "\n\n=== 终端输出 ===\n" + terminalOutput;
                            }
                        }
                        
                        if (combinedLogContent != LogContent)
                        {
                            LogContent = combinedLogContent;
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
