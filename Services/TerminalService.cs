using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ProjectManager.Models;
using ProjectManager.Helpers;

namespace ProjectManager.Services
{
    /// <summary>
    /// 终端服务类
    /// </summary>
    public class TerminalService
    {
        private readonly Dictionary<string, TerminalSession> _sessions = new();
        private readonly object _lockObject = new();
        private readonly ISettingsService _settingsService;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly ILanguageService _languageService;

        public TerminalService(ISettingsService settingsService, IErrorDisplayService errorDisplayService, ILanguageService languageService)
        {
            _settingsService = settingsService;
            _errorDisplayService = errorDisplayService;
            _languageService = languageService;

            _languageService.LanguageChanged += (s, e) =>
            {
                lock (_lockObject)
                {
                    foreach (var session in _sessions.Values)
                    {
                        session.RefreshStatus();
                    }
                }
            };
        }

        /// <summary>
        /// 尝试定位由 shell 进程启动的真实子进程。
        /// </summary>
        private static Process? TryGetChildProcess(Process process)
        {
            try
            {
                return ProcessInterop.TryResolveRealProcess(process);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 创建新的终端会话
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <param name="projectPath">项目路径</param>
        /// <param name="command">启动命令</param>
        /// <param name="environmentVariables">环境变量</param>
        /// <returns>终端会话</returns>
        public TerminalSession CreateSession(string projectName, string projectPath, string command, Dictionary<string, string>? environmentVariables = null)
        {
            lock (_lockObject)
            {
                var session = new TerminalSession
                {
                    ProjectName = projectName,
                    ProjectPath = projectPath,
                    Command = command,
                    StartTime = DateTime.Now,
                    EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>()
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
        /// <param name="environmentVariables">环境变量</param>
        /// <returns>是否启动成功</returns>
        public async Task<bool> StartSessionAsync(TerminalSession session, Dictionary<string, string>? environmentVariables = null)
        {
            try
            {
                if (session.IsRunning)
                {
                    var s0 = await _settingsService.GetSettingsAsync();
                    session.AddOutputRawWithTimestamp(_languageService.GetString("Terminal_AlreadyRunning") + "\r\n", s0.ShowTerminalTimestamps);
                    return false;
                }

                session.UpdateStatus(TerminalStatus.Starting, false);
                var settings = await _settingsService.GetSettingsAsync();
                session.AddOutputRawWithTimestamp($"{_languageService.GetString("Terminal_StartCommand")}{session.Command}\r\n", settings.ShowTerminalTimestamps);
                session.AddOutputRawWithTimestamp($"{_languageService.GetString("Terminal_WorkingDir")} {session.ProjectPath}\r\n", settings.ShowTerminalTimestamps);

                // 设置环境变量 - 优先使用传入的环境变量，否则使用会话中存储的环境变量（复制避免外部被修改）
                var envVars = (environmentVariables ?? session.EnvironmentVariables) != null
                    ? new Dictionary<string, string>(environmentVariables ?? session.EnvironmentVariables)
                    : new Dictionary<string, string>();

                // 获取用户设置的终端类型（包含是否显示时间戳）
                // var settings 已在上方获取

                // 构建完整的命令序列（根据终端类型生成对应的环境变量设置方式）
                var commandSequence = new List<string>();
                if (envVars != null && envVars.Any())
                {
                    session.AddOutputRawWithTimestamp(_languageService.GetString("Terminal_SettingEnvVars") + "\r\n", settings.ShowTerminalTimestamps);
                    foreach (var env in envVars)
                    {
                        session.AddOutputRawWithTimestamp($"  {env.Key}={env.Value}\r\n", settings.ShowTerminalTimestamps);
                    }
                    commandSequence.AddRange(BuildEnvCommands(settings.PreferredTerminal, envVars));
                }

                // 添加启动命令
                if (!string.IsNullOrWhiteSpace(session.Command))
                {
                    commandSequence.Add(session.Command);
                }

                var (fileName, arguments) = GetTerminalCommandWithSequence(settings.PreferredTerminal, commandSequence);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = session.ProjectPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                // 双重保障：除了命令级set，还进行进程级环境变量注入
                if (envVars != null && envVars.Any())
                {
                    foreach (var kv in envVars)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Key))
                            {
                                // 若已存在同名变量，覆盖设置
                                processStartInfo.Environment[kv.Key] = kv.Value ?? string.Empty;
                            }
                        }
                        catch { /* 忽略单个变量注入错误 */ }
                    }
                }

                var process = new Process { StartInfo = processStartInfo };
                session.Process = process; // initially the shell process

                var cts = new CancellationTokenSource();
                async Task ReadStreamAsync(Stream stream)
                {
                    var buffer = new byte[4096];
                    while (!cts.IsCancellationRequested)
                    {
                        int bytesRead;
                        try
                        {
                            bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch
                        {
                            break;
                        }
                        if (bytesRead <= 0) break;
                        var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        if (!string.IsNullOrEmpty(text))
                        {
                            session.AddOutputRawWithTimestamp(text, settings.ShowTerminalTimestamps);
                        }
                    }
                }

                // 处理进程退出
                process.Exited += (sender, e) =>
                {
                    session.UpdateStatus(TerminalStatus.Stopped, false);
                    session.AddOutputRawWithTimestamp($"{_languageService.GetString("Terminal_ProcessExited")}{process.ExitCode}\r\n", settings.ShowTerminalTimestamps);
                    try { cts.Cancel(); } catch { }
                };

                process.EnableRaisingEvents = true;
                process.Start();

                // Try to detect if the shell spawns a child process that is the real app
                // We'll poll synchronously for a short period so callers (e.g. ProjectService) can get the real process
                try
                {
                    const int maxAttempts = 10;
                    const int delayMs = 150;
                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        await Task.Delay(delayMs);
                        var child = TryGetChildProcess(process);
                        if (child != null && child.Id != process.Id)
                        {
                            // Found a child process; set as session.Process for monitoring
                            try
                            {
                                child.EnableRaisingEvents = true;
                            }
                            catch { }
                            session.Process = child;
                            session.AddOutputRawWithTimestamp(string.Format(_languageService.GetString("Terminal_ChildProcessDetected"), child.Id, child.ProcessName) + "\r\n", settings.ShowTerminalTimestamps);
                            break;
                        }
                    }
                }
                catch { }
                _ = ReadStreamAsync(process.StandardOutput.BaseStream);
                _ = ReadStreamAsync(process.StandardError.BaseStream);

                session.UpdateStatus(TerminalStatus.Running, true);
                session.AddOutputRawWithTimestamp(_languageService.GetString("Terminal_Started") + "\r\n", settings.ShowTerminalTimestamps);

                return true;
            }
            catch (Exception ex)
            {
                session.UpdateStatus(TerminalStatus.StartFailed, false);
                var s1 = await _settingsService.GetSettingsAsync();
                session.AddOutputRawWithTimestamp(string.Format(_languageService.GetString("Terminal_StartFailedMessage"), ex.Message) + "\r\n", s1.ShowTerminalTimestamps);
                // 显示终端启动错误
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync(string.Format(_languageService.GetString("Terminal_StartFailedMessage"), ex.Message), _languageService.GetString("Terminal_StartError")));
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
                    try
                    {
                        // First attempt: use managed Kill with tree support
                        session.Process.Kill(entireProcessTree: true);
                    }
                    catch { }

                    // Wait briefly for process to exit
                    try
                    {
                        if (!session.Process.WaitForExit(50))
                        {
                            // Fallback to taskkill to forcefully terminate the process tree
                            try
                            {
                                var proc = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = "taskkill",
                                        Arguments = $"/PID {session.Process.Id} /F /T",
                                        CreateNoWindow = true,
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true
                                    }
                                };
                                proc.Start();
                                proc.WaitForExit(2000);
                            }
                            catch { }
                        }
                    }
                    catch { }

                    var s2 = _settingsService.GetSettingsAsync().Result;
                    session.AddOutputRawWithTimestamp(_languageService.GetString("Terminal_ForceStopped") + "\r\n", s2.ShowTerminalTimestamps);
                }
                session.UpdateStatus(TerminalStatus.Stopped, false);
            }
            catch (Exception ex)
            {
                var s3 = _settingsService.GetSettingsAsync().Result;
                session.AddOutputRawWithTimestamp(string.Format(_languageService.GetString("Terminal_StopFailedMessage"), ex.Message) + "\r\n", s3.ShowTerminalTimestamps);
                // 显示终端停止错误
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync(string.Format(_languageService.GetString("Terminal_StopFailedMessage"), ex.Message), _languageService.GetString("Terminal_StopError")));
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

        /// <summary>
        /// 修复编码问题
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>修复后的字符串</returns>
        private string FixEncodingIssues(string input)
        {
            try
            {
                // 如果字符串包含乱码字符，尝试重新编码
                if (input.Contains("�") || HasGarbledCharacters(input))
                {
                    // 尝试从GBK转换到UTF-8
                    var gbkEncoding = Encoding.GetEncoding("GBK");
                    var utf8Encoding = Encoding.UTF8;
                    
                    // 先转换为字节数组，再用正确的编码解码
                    var bytes = Encoding.Default.GetBytes(input);
                    return gbkEncoding.GetString(bytes);
                }
                return input;
            }
            catch
            {
                // 如果转换失败，返回原字符串
                return input;
            }
        }

        /// <summary>
        /// <summary>
        /// 检查是否包含乱码字符
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>是否包含乱码</returns>
        private bool HasGarbledCharacters(string input)
        {
            // 检查是否包含常见的乱码模式
            return input.Contains("��") || 
                   input.Contains("锘�") ||
                   System.Text.RegularExpressions.Regex.IsMatch(input, @"[^\x00-\x7F\u4e00-\u9fff\u3400-\u4dbf\uf900-\ufaff\u3040-\u309f\u30a0-\u30ff]");
        }

        /// <summary>
        /// 根据终端类型获取对应的命令序列
        /// </summary>
        /// <param name="terminalType">终端类型</param>
        /// <param name="commandSequence">要执行的命令序列</param>
        /// <returns>终端可执行文件名和参数</returns>
        private (string fileName, string arguments) GetTerminalCommandWithSequence(string terminalType, List<string> commandSequence)
        {
            if (commandSequence == null || !commandSequence.Any())
            {
                return ("cmd.exe", "/c echo No commands to execute");
            }

            var settings = _settingsService.GetSettingsAsync().Result;
            var cmdPrefix = settings.UseCmdChcp65001 ? "chcp 65001 && " : "";

            return terminalType?.ToLower() switch
            {
                "powershell" or "powershell 7" => 
                    ("powershell.exe", $"-NoProfile -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; {string.Join("; ", commandSequence)}\""),
                
                "cmd" or "command prompt" => 
                    ("cmd.exe", $"/c {cmdPrefix}{string.Join(" && ", commandSequence)}"),
                
                "git bash" => 
                    ("bash.exe", $"-c \"export LANG=en_US.UTF-8; export LC_ALL=en_US.UTF-8; export TERM=xterm-256color; {string.Join(" && ", commandSequence)}\""),
                
                _ => // 默认使用 PowerShell
                    ("powershell.exe", $"-NoProfile -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; {string.Join("; ", commandSequence)}\"")
            };
        }

        /// <summary>
        /// 将环境变量转换为目标终端的设置命令
        /// </summary>
        private static IEnumerable<string> BuildEnvCommands(string terminalType, Dictionary<string, string> env)
        {
            var type = terminalType?.ToLower() ?? "powershell";
            switch (type)
            {
                case "cmd":
                case "command prompt":
                    foreach (var kv in env)
                    {
                        var key = EscapeCmd(kv.Key);
                        var val = EscapeCmd(kv.Value ?? string.Empty);
                        yield return $"set \"{key}={val}\"";
                    }
                    break;
                case "git bash":
                    foreach (var kv in env)
                    {
                        var key = EscapeBash(kv.Key);
                        var val = EscapeBash(kv.Value ?? string.Empty);
                        yield return $"export {key}=\"{val}\"";
                    }
                    break;
                default: // powershell
                    foreach (var kv in env)
                    {
                        var key = EscapePwsh(kv.Key);
                        var val = EscapePwsh(kv.Value ?? string.Empty);
                        // 使用单引号包裹值，避免在 -Command 外层双引号中冲突
                        yield return $"$env:{key} = '{val}'";
                    }
                    break;
            }

            static string EscapeCmd(string s) => s.Replace("\"", "\\\"");
            static string EscapeBash(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            // 在单引号字符串中，PowerShell 通过重复单引号来转义
            static string EscapePwsh(string s) => s.Replace("'", "''");
        }

        /// <summary>
        /// 根据终端类型获取对应的命令
        /// </summary>
        /// <param name="terminalType">终端类型</param>
        /// <param name="command">要执行的命令</param>
        /// <returns>终端可执行文件名和参数</returns>
        private (string fileName, string arguments) GetTerminalCommand(string terminalType, string command)
        {
            var settings = _settingsService.GetSettingsAsync().Result;
            var cmdPrefix = settings.UseCmdChcp65001 ? "chcp 65001 && " : "";

            return terminalType?.ToLower() switch
            {
                "powershell" or "powershell 7" => 
                    ("powershell.exe", $"-NoProfile -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; {command}\""),
                
                "cmd" or "command prompt" => 
                    ("cmd.exe", $"/c {cmdPrefix}{command}"),
                
                "git bash" => 
                    ("bash.exe", $"-c \"{command}\""),
                
                _ => // 默认使用 PowerShell
                    ("powershell.exe", $"-NoProfile -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; {command}\"")
            };
        }
    }
}
