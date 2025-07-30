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
        private readonly System.Windows.Threading.DispatcherTimer _syncTimer;

        [ObservableProperty]
        private ObservableCollection<TerminalSession> _terminalSessions = new();

        [ObservableProperty]
        private TerminalSession? _selectedSession;

        [ObservableProperty]
        private string _selectedOutput = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        public TerminalViewModel(TerminalService terminalService, IProjectService projectService)
        {
            _terminalService = terminalService;
            _projectService = projectService;
            
            // 设置同步定时器，每2秒同步一次项目状态
            _syncTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _syncTimer.Tick += async (s, e) => await SyncProjectStatesAsync();
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

            IsLoading = true;
            try
            {
                await _terminalService.StartSessionAsync(SelectedSession);
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
            var session = _terminalService.CreateSession(projectName, projectPath, command);
            TerminalSessions.Add(session);
            SelectedSession = session;
            
            await _terminalService.StartSessionAsync(session);
        }

        /// <summary>
        /// 同步项目状态
        /// </summary>
        private async Task SyncProjectStatesAsync()
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
            }
        }
    }
}