using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Models;
using ProjectManager.Services;
using Wpf.Ui.Abstractions.Controls;
using System.Diagnostics;

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
                var project = await FindProjectByNameAsync(SelectedSession.ProjectName);
                var envForLaunch = BuildEnvironmentVariables(project);
                await StartSessionWithProjectTrackingAsync(SelectedSession, project, envForLaunch);
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
        private async Task StopTerminal()
        {
            if (SelectedSession == null) return;

            var projectName = SelectedSession.ProjectName;
            await UpdateProjectStatusAsync(projectName, SelectedSession.Process, ProjectStatus.Stopping);
            _terminalService.StopSession(SelectedSession);
            await UpdateProjectStatusAsync(projectName, null, ProjectStatus.Stopped);
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
        /// 在外部打开命令提示符（CMD），起始目录为当前选中会话的工作目录
        /// </summary>
        [RelayCommand]
        private void OpenCmd()
        {
            try
            {
                var dir = SelectedSession?.ProjectPath;
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                {
                    dir = Environment.CurrentDirectory;
                }

                // 尝试检测并构建在打开的 cmd 中执行的初始化命令（激活虚拟环境、设置提示符、替换 pip 为 python -m pip）
                var project = Task.Run(() => FindProjectByNameAsync(SelectedSession?.ProjectName)).Result;
                var initCmd = BuildCmdInitialization(dir, project);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    // 如果有初始化命令，则通过 /k "..." 在新窗口中执行并保留窗口；否则直接打开工作目录
                    Arguments = string.IsNullOrEmpty(initCmd) ? "/k" : $"/k \"{initCmd}\"",
                    WorkingDirectory = dir,
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"无法打开命令提示符: {ex.Message}", "打开 CMD 失败"));
            }
        }

        /// <summary>
        /// 为 CMD 窗口构建初始化命令（激活项目虚拟环境并设置提示符与 pip 别名）
        /// 返回可以直接放入 cmd.exe /k "..." 的命令串，或为空表示无需特殊初始化
        /// </summary>
        private string? BuildCmdInitialization(string projectDir, Project? project)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                    return null;

                // 首先检查项目设置中是否显式指定了 Python 可执行文件（例如 ComfyUI 的 PythonPath）并优先使用它
                var pyPath = project?.ComfyUISettings?.PythonPath;
                if (!string.IsNullOrWhiteSpace(pyPath) && File.Exists(pyPath))
                {
                    var scriptsDir = Path.GetDirectoryName(pyPath);
                    if (!string.IsNullOrWhiteSpace(scriptsDir))
                    {
                        var setPath = $"set \"PATH={scriptsDir};%PATH%\"";
                        var promptCmd = $"prompt ({scriptsDir}) $P$G";
                        var doskey = "doskey pip=python -m pip $*";
                        return string.Join(" && ", new List<string> { setPath, promptCmd, doskey });
                    }

                    var pythonParent = Path.GetDirectoryName(pyPath) ?? projectDir;
                    var rootName = Path.GetFileName(pythonParent) ?? "python";
                    var doskeyCmd = $"doskey pip=\"{pyPath}\" -m pip $*";
                    var simpleParts = new List<string> { $"prompt ({rootName}) $P$G", doskeyCmd };
                    return string.Join(" && ", simpleParts);
                }

                var venvCandidates = new[] { ".venv", "venv", "env", ".env", "venv3", "virtualenv" };
                foreach (var name in venvCandidates)
                {
                    var scriptsDir = Path.Combine(projectDir, name, "Scripts");
                    var pythonExe = Path.Combine(scriptsDir, "python.exe");
                    if (Directory.Exists(scriptsDir) && File.Exists(pythonExe))
                    {
                        var setPath = $"set \"PATH={scriptsDir};%PATH%\"";
                        var promptCmd = $"prompt ({scriptsDir}) $P$G";
                        var doskey = "doskey pip=python -m pip $*";
                        var parts = new List<string> { setPath, promptCmd, doskey };
                        return string.Join(" && ", parts);
                    }
                }

                // 未找到虚拟环境或指定的 Python，可尝试查找 pip 在项目 Scripts 下（宽松匹配）
                foreach (var dirName in venvCandidates)
                {
                    var scriptsDir = Path.Combine(projectDir, dirName, "Scripts");
                    var maybePython = Path.Combine(scriptsDir, "python.exe");
                    if (Directory.Exists(scriptsDir) && File.Exists(maybePython))
                    {
                        var setPath = $"set \"PATH={scriptsDir};%PATH%\"";
                        var promptCmd = $"prompt ({scriptsDir}) $P$G";
                        var doskey = $"doskey pip=python -m pip $*";
                        return string.Join(" && ", new List<string> { setPath, promptCmd, doskey });
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
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
            
            var project = await FindProjectByNameAsync(projectName);
            var envForLaunch2 = BuildEnvironmentVariables(project);
            await StartSessionWithProjectTrackingAsync(session, project, envForLaunch2);
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

        private async Task<Project?> FindProjectByNameAsync(string? projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return null;

            var projects = await _projectService.GetProjectsAsync();
            return projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        }

        private Dictionary<string, string>? BuildEnvironmentVariables(Project? project)
        {
            if (project == null)
                return null;

            var env = new Dictionary<string, string>(project.EnvironmentVariables ?? new Dictionary<string, string>());

            if (!string.IsNullOrWhiteSpace(project.Framework) && project.Framework.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase))
            {
                env["PYTHONUTF8"] = "1";
                env["PYTHONIOENCODING"] = "UTF-8";
            }

            return env;
        }

        private async Task StartSessionWithProjectTrackingAsync(TerminalSession session, Project? project, Dictionary<string, string>? environmentVariables)
        {
            string? projectName = project?.Name ?? session.ProjectName;

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                await UpdateProjectStatusAsync(projectName, null, ProjectStatus.Starting);
            }

            var started = await _terminalService.StartSessionAsync(session, environmentVariables);

            if (string.IsNullOrWhiteSpace(projectName))
            {
                return;
            }

            if (started)
            {
                await UpdateProjectStatusAsync(projectName, session.Process, ProjectStatus.Running);
                AttachProcessExitHandler(session, projectName);
            }
            else
            {
                await UpdateProjectStatusAsync(projectName, null, ProjectStatus.Error);
            }
        }

        private Task UpdateProjectStatusAsync(string? projectName, Process? process, ProjectStatus status)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return Task.CompletedTask;

            return _projectService.UpdateProjectRuntimeStatusAsync(projectName, process, status);
        }

        private void AttachProcessExitHandler(TerminalSession session, string projectName)
        {
            var process = session.Process;
            if (process == null)
                return;

            void Handler(object? sender, EventArgs args)
            {
                process.Exited -= Handler;
                _ = _projectService.UpdateProjectRuntimeStatusAsync(projectName, null, ProjectStatus.Stopped);
            }

            try
            {
                process.Exited -= Handler;
            }
            catch { }

            process.Exited += Handler;
        }
    }
}