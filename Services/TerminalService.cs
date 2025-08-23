using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;
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
        private readonly ISettingsService _settingsService;
        private readonly IErrorDisplayService _errorDisplayService;

        public TerminalService(ISettingsService settingsService, IErrorDisplayService errorDisplayService)
        {
            _settingsService = settingsService;
            _errorDisplayService = errorDisplayService;
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
                    session.AddOutputRaw("终端已在运行中\r\n");
                    return false;
                }

                session.UpdateStatus("正在启动...", false);
                session.AddOutputRaw($"启动命令: {session.Command}\r\n");
                session.AddOutputRaw($"工作目录: {session.ProjectPath}\r\n");

                // 设置环境变量 - 优先使用传入的环境变量，否则使用会话中存储的环境变量（复制避免外部被修改）
                var envVars = (environmentVariables ?? session.EnvironmentVariables) != null
                    ? new Dictionary<string, string>(environmentVariables ?? session.EnvironmentVariables)
                    : new Dictionary<string, string>();

                // 获取用户设置的终端类型
                var settings = await _settingsService.GetSettingsAsync();

                // 构建完整的命令序列（根据终端类型生成对应的环境变量设置方式）
                var commandSequence = new List<string>();
                if (envVars != null && envVars.Any())
                {
                    session.AddOutputRaw("设置环境变量:\r\n");
                    foreach (var env in envVars)
                    {
                        session.AddOutputRaw($"  {env.Key}={env.Value}\r\n");
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
                session.Process = process;

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
                            session.AddOutputRaw(text);
                        }
                    }
                }

                // 处理进程退出
                process.Exited += (sender, e) =>
                {
                    session.UpdateStatus("已停止", false);
                    session.AddOutputRaw($"进程已退出，退出代码: {process.ExitCode}\r\n");
                    try { cts.Cancel(); } catch { }
                };

                process.EnableRaisingEvents = true;
                process.Start();
                _ = ReadStreamAsync(process.StandardOutput.BaseStream);
                _ = ReadStreamAsync(process.StandardError.BaseStream);

                session.UpdateStatus("运行中", true);
                session.AddOutputRaw("终端已启动\r\n");

                return true;
            }
            catch (Exception ex)
            {
                session.UpdateStatus("启动失败", false);
                session.AddOutputRaw($"启动失败: {ex.Message}\r\n");
                // 显示终端启动错误
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"终端启动失败: {ex.Message}", "终端启动错误"));
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
                    session.AddOutputRaw("终端已强制停止\r\n");
                }
                session.UpdateStatus("已停止", false);
            }
            catch (Exception ex)
            {
                session.AddOutputRaw($"停止失败: {ex.Message}\r\n");
                // 显示终端停止错误
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"终端停止失败: {ex.Message}", "终端停止错误"));
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
