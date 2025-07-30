using System.Diagnostics;
using System.Text;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    /// <summary>
    /// 终端服务类
    /// </summary>
    public class TerminalService
    {
        private readonly Dictionary<string, TerminalSession> _sessions = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// 创建新的终端会话
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <param name="projectPath">项目路径</param>
        /// <param name="command">启动命令</param>
        /// <returns>终端会话</returns>
        public TerminalSession CreateSession(string projectName, string projectPath, string command)
        {
            lock (_lockObject)
            {
                var session = new TerminalSession
                {
                    ProjectName = projectName,
                    ProjectPath = projectPath,
                    Command = command,
                    StartTime = DateTime.Now
                };

                _sessions[session.SessionId] = session;
                return session;
            }
        }

        /// <summary>
        /// 获取终端会话
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns>终端会话</returns>
        public TerminalSession? GetSession(string sessionId)
        {
            lock (_lockObject)
            {
                return _sessions.TryGetValue(sessionId, out var session) ? session : null;
            }
        }

        /// <summary>
        /// 获取所有会话
        /// </summary>
        /// <returns>会话列表</returns>
        public List<TerminalSession> GetAllSessions()
        {
            lock (_lockObject)
            {
                return _sessions.Values.ToList();
            }
        }

        /// <summary>
        /// 启动终端会话
        /// </summary>
        /// <param name="session">终端会话</param>
        /// <returns>是否启动成功</returns>
        public async Task<bool> StartSessionAsync(TerminalSession session)
        {
            try
            {
                if (session.IsRunning)
                {
                    session.AddOutputLine("终端已在运行中");
                    return false;
                }

                session.UpdateStatus("正在启动...", false);
                session.AddOutputLine($"启动命令: {session.Command}");
                session.AddOutputLine($"工作目录: {session.ProjectPath}");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {session.Command}",
                    WorkingDirectory = session.ProjectPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var process = new Process { StartInfo = processStartInfo };
                session.Process = process;

                // 处理标准输出
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        session.AddOutputLine(e.Data);
                    }
                };

                // 处理错误输出
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        session.AddOutputLine($"ERROR: {e.Data}");
                    }
                };

                // 处理进程退出
                process.Exited += (sender, e) =>
                {
                    session.UpdateStatus("已停止", false);
                    session.AddOutputLine($"进程已退出，退出代码: {process.ExitCode}");
                };

                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                session.UpdateStatus("运行中", true);
                session.AddOutputLine("终端已启动");

                return true;
            }
            catch (Exception ex)
            {
                session.UpdateStatus("启动失败", false);
                session.AddOutputLine($"启动失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止终端会话
        /// </summary>
        /// <param name="session">终端会话</param>
        public void StopSession(TerminalSession session)
        {
            try
            {
                if (session.Process != null && !session.Process.HasExited)
                {
                    session.Process.Kill(true);
                    session.AddOutputLine("终端已强制停止");
                }
                session.UpdateStatus("已停止", false);
            }
            catch (Exception ex)
            {
                session.AddOutputLine($"停止失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除会话
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        public void RemoveSession(string sessionId)
        {
            lock (_lockObject)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    StopSession(session);
                    _sessions.Remove(sessionId);
                }
            }
        }

        /// <summary>
        /// 清理所有会话
        /// </summary>
        public void Cleanup()
        {
            lock (_lockObject)
            {
                foreach (var session in _sessions.Values)
                {
                    StopSession(session);
                }
                _sessions.Clear();
            }
        }
    }
}