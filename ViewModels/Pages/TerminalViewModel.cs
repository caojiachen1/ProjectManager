using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Models;
using ProjectManager.Services;
using Wpf.Ui.Abstractions.Controls;

namespace ProjectManager.ViewModels.Pages
{
    /// <summary>
    /// 终端页面视图模型
    /// </summary>
    public partial class TerminalViewModel : ObservableObject, INavigationAware
    {
        private readonly TerminalService _terminalService;
        private readonly IProjectService _projectService;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly System.Windows.Threading.DispatcherTimer _syncTimer;

        [ObservableProperty]
        private ObservableCollection<TerminalSession> _terminalSessions = new();

        [ObservableProperty]
        private TerminalSession? _selectedSession;

        [ObservableProperty]
        private string _selectedOutput = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        // 用于在导航时指定要切换到的项目
        private string? _pendingProjectName;
        private string? _pendingProjectPath;
        private string? _pendingStartCommand;

        public TerminalViewModel(TerminalService terminalService, IProjectService projectService, IErrorDisplayService errorDisplayService)
        {
            _terminalService = terminalService;
            _projectService = projectService;
            _errorDisplayService = errorDisplayService;
            
            // 设置同步定时器，每2秒同步一次项目状态
            _syncTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _syncTimer.Tick += (s, e) => SyncProjectStates();
        }

        public void OnNavigatedTo()
        {
            LoadTerminalSessions();
            _syncTimer.Start(); // 启动同步定时器
        }

        public void OnNavigatedFrom()
        {
            _syncTimer.Stop(); // 停止同步定时器
        }

        public async Task OnNavigatedToAsync()
        {
            LoadTerminalSessions();

            // 处理待切换的项目
            if (!string.IsNullOrEmpty(_pendingProjectName))
            {
                // 解析项目的默认启动命令（若未显式传入）
                string? resolvedCommand = _pendingStartCommand;
                string? resolvedPath = _pendingProjectPath;
                try
                {
                    var projects = await _projectService.GetProjectsAsync();
                    var proj = projects.FirstOrDefault(p => p.Name == _pendingProjectName);
                    if (string.IsNullOrWhiteSpace(resolvedCommand))
                        resolvedCommand = proj?.StartCommand;
                    if (string.IsNullOrWhiteSpace(resolvedPath))
                        resolvedPath = !string.IsNullOrWhiteSpace(proj?.WorkingDirectory) ? proj!.WorkingDirectory : proj?.LocalPath;
                }
                catch { /* 忽略项目获取失败，保持传入值 */ }

                // 检查是否已存在该项目的终端会话
                var existingSession = TerminalSessions.FirstOrDefault(s => s.ProjectName == _pendingProjectName);
                if (existingSession != null)
                {
                    // 更新现有会话的命令和路径（若可用）
                    if (!string.IsNullOrWhiteSpace(resolvedCommand))
                        existingSession.Command = resolvedCommand!;
                    if (!string.IsNullOrWhiteSpace(resolvedPath))
                        existingSession.ProjectPath = resolvedPath!;
                    // 切换到现有会话
                    SelectedSession = existingSession;
                }
                else if (!string.IsNullOrWhiteSpace(resolvedPath))
                {
                    // 创建会话但不自动启动：预填充命令，便于用户点击“启动”立即可用
                    var session = _terminalService.CreateSession(_pendingProjectName, resolvedPath!, resolvedCommand ?? string.Empty);
                    TerminalSessions.Add(session);
                    SelectedSession = session;
                }

                // 清空待处理的项目信息
                _pendingProjectName = null;
                _pendingProjectPath = null;
                _pendingStartCommand = null;
            }

            await Task.CompletedTask;
        }

        public async Task OnNavigatedFromAsync()
        {
            // 页面离开时的清理工作
            await Task.CompletedTask;
        }

        /// <summary>
        /// 加载终端会话
        /// </summary>
        private void LoadTerminalSessions()
        {
            IsLoading = true;
            
            try
            {
                var sessions = _terminalService.GetAllSessions();
                TerminalSessions.Clear();
                
                foreach (var session in sessions)
                {
                    TerminalSessions.Add(session);
                }

                // 如果有会话，选择第一个
                if (TerminalSessions.Count > 0)
                {
                    SelectedSession = TerminalSessions[0];
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 选择会话时更新输出显示
        /// </summary>
        partial void OnSelectedSessionChanged(TerminalSession? value)
        {
            // UI现在直接绑定到OutputLines，不需要额外处理
        }

        /// <summary>
        /// 启动终端命令
        /// </summary>
        [RelayCommand]
        private async Task StartTerminalAsync()
        {
            if (SelectedSession == null) return;

            if (string.IsNullOrWhiteSpace(SelectedSession.Command))
            {
                await _errorDisplayService.ShowErrorAsync("当前会话未配置启动命令，请先在项目中设置启动命令。", "无法启动");
                return;
            }

            IsLoading = true;
            try
            {
                // 根据终端会话的项目名称获取对应的项目环境变量
                Dictionary<string, string>? projectEnvironmentVariables = null;
                
                if (!string.IsNullOrEmpty(SelectedSession.ProjectName))
                {
                    var projects = await _projectService.GetProjectsAsync();
                    var matchingProject = projects.FirstOrDefault(p => p.Name == SelectedSession.ProjectName);
                    if (matchingProject != null)
                    {
                        projectEnvironmentVariables = matchingProject.EnvironmentVariables;
                    }
                }

                // 启动会话时传递项目的环境变量，确保与项目卡片启动效果一致
                // 若为 ComfyUI 项目，则自动注入 Python UTF-8（其他项目不注入，不做任何提示）
                Dictionary<string, string>? envForLaunch = projectEnvironmentVariables;
                if (!string.IsNullOrEmpty(SelectedSession.ProjectName))
                {
                    var projects2 = await _projectService.GetProjectsAsync();
                    var proj2 = projects2.FirstOrDefault(p => p.Name == SelectedSession.ProjectName);
                    if (proj2 != null && !string.IsNullOrWhiteSpace(proj2.Framework) && proj2.Framework.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase))
                    {
                        envForLaunch = new Dictionary<string, string>(projectEnvironmentVariables ?? new Dictionary<string, string>())
                        {
                            ["PYTHONUTF8"] = "1",
                            ["PYTHONIOENCODING"] = "UTF-8"
                        };
                    }
                }
                await _terminalService.StartSessionAsync(SelectedSession, envForLaunch);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 停止终端
        /// </summary>
        [RelayCommand]
        private void StopTerminal()
        {
            if (SelectedSession == null) return;

            _terminalService.StopSession(SelectedSession);
        }

        /// <summary>
        /// 清空输出
        /// </summary>
        [RelayCommand]
        private void ClearOutput()
        {
            SelectedSession?.ClearOutput();
        }

        /// <summary>
        /// 关闭会话
        /// </summary>
        [RelayCommand]
        private void CloseSession(TerminalSession? session)
        {
            if (session == null) return;

            _terminalService.RemoveSession(session.SessionId);
            TerminalSessions.Remove(session);

            // 如果关闭的是当前选中的会话，选择下一个
            if (SelectedSession == session)
            {
                SelectedSession = TerminalSessions.FirstOrDefault();
            }
        }

        /// <summary>
        /// 刷新会话列表
        /// </summary>
        [RelayCommand]
        private void RefreshSessions()
        {
            LoadTerminalSessions();
        }

        /// <summary>
        /// 设置导航到终端页面时要切换的项目
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <param name="projectPath">项目路径</param>
        /// <param name="startCommand">启动命令</param>
        public void SetPendingProject(string projectName, string projectPath, string startCommand)
        {
            _pendingProjectName = projectName;
            _pendingProjectPath = projectPath;
            _pendingStartCommand = startCommand;
        }

        /// <summary>
        /// 设置项目路径但不自动启动
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <param name="projectPath">项目路径</param>
        public void SetProjectPath(string projectName, string projectPath)
        {
            _pendingProjectName = projectName;
            _pendingProjectPath = projectPath;
            _pendingStartCommand = null; // 不设置启动命令，避免自动启动
        }

        /// <summary>
        /// 根据项目名称切换到对应的终端会话
        /// </summary>
        /// <param name="projectName">项目名称</param>
        public void SwitchToProjectTerminal(string projectName)
        {
            var session = TerminalSessions.FirstOrDefault(s => s.ProjectName == projectName);
            if (session != null)
            {
                SelectedSession = session;
            }
        }

        /// <summary>
        /// 创建新的终端会话
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <param name="projectPath">项目路径</param>
        /// <param name="command">启动命令</param>
        public async Task CreateAndStartSessionAsync(string projectName, string projectPath, string command)
        {
            // 检查是否已存在同名项目的会话
            var existingSession = TerminalSessions.FirstOrDefault(s => s.ProjectName == projectName);
            
            TerminalSession session;
            if (existingSession != null)
            {
                // 更新现有会话的命令和路径
                session = existingSession;
                session.Command = command;
                session.ProjectPath = projectPath;
                SelectedSession = session;
            }
            else
            {
                // 创建新会话
                session = _terminalService.CreateSession(projectName, projectPath, command);
                TerminalSessions.Add(session);
                SelectedSession = session;
            }
            
            // 获取项目的环境变量
            Dictionary<string, string>? projectEnvironmentVariables = null;
            if (!string.IsNullOrEmpty(projectName))
            {
                var projects = await _projectService.GetProjectsAsync();
                var matchingProject = projects.FirstOrDefault(p => p.Name == projectName);
                if (matchingProject != null)
                {
                    projectEnvironmentVariables = matchingProject.EnvironmentVariables;
                }
            }
            
            // 启动会话时传递项目的环境变量
            Dictionary<string, string>? envForLaunch2 = projectEnvironmentVariables;
            if (!string.IsNullOrEmpty(projectName))
            {
                var projects3 = await _projectService.GetProjectsAsync();
                var proj3 = projects3.FirstOrDefault(p => p.Name == projectName);
                if (proj3 != null && !string.IsNullOrWhiteSpace(proj3.Framework) && proj3.Framework.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase))
                {
                    envForLaunch2 = new Dictionary<string, string>(projectEnvironmentVariables ?? new Dictionary<string, string>())
                    {
                        ["PYTHONUTF8"] = "1",
                        ["PYTHONIOENCODING"] = "UTF-8"
                    };
                }
            }
            await _terminalService.StartSessionAsync(session, envForLaunch2);
        }

        /// <summary>
        /// 同步项目状态
        /// </summary>
        private void SyncProjectStates()
        {
            try
            {
                foreach (var session in TerminalSessions)
                {
                    // 根据进程状态更新会话状态
                    if (session.Process != null)
                    {
                        if (session.Process.HasExited)
                        {
                            session.UpdateStatus("已停止", false);
                        }
                        else
                        {
                            session.UpdateStatus("运行中", true);
                        }
                    }
                    else
                    {
                        session.UpdateStatus("未启动", false);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响UI
                System.Diagnostics.Debug.WriteLine($"同步项目状态失败: {ex.Message}");
                // 只在关键错误时显示给用户
                if (ex is not TimeoutException)
                {
                    _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"同步项目状态失败: {ex.Message}", "同步错误"));
                }
            }
        }
    }
}