using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    public class EnvironmentVariableService
    {
        /// <summary>
        /// 设置用户环境变量（不需要管理员权限）
        /// </summary>
        public bool SetUserVariable(string name, string value)
        {
            try
            {
                // 使用 .NET 内置方法设置用户环境变量
                Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置用户环境变量失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置系统环境变量（需要管理员权限）
        /// </summary>
        public bool SetSystemVariable(string name, string value)
        {
            try
            {
                // 使用setx命令设置系统环境变量
                string escapedValue = EscapeArgument(value);
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "setx",
                    Arguments = $"\"{name}\" \"{escapedValue}\" /M",
                    Verb = "runas", // 请求管理员权限
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                using (var process = Process.Start(startInfo))
                {
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置系统环境变量失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用UAC提权设置系统环境变量
        /// </summary>
        public bool SetSystemVariableWithUac(string name, string value)
        {
            try
            {
                string escapedValue = EscapeArgument(value);
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "setx",
                    Arguments = $"\"{name}\" \"{escapedValue}\" /M",
                    Verb = "runas", // 触发UAC
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // 用户取消了UAC提示
                Debug.WriteLine("用户取消了UAC提权");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UAC设置系统环境变量失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除环境变量
        /// </summary>
        public bool DeleteVariable(string name, bool isSystemVariable)
        {
            try
            {
                if (isSystemVariable)
                {
                    // 使用setx设置空值来删除系统环境变量，需要UAC
                    return DeleteSystemVariableWithUac(name);
                }
                else
                {
                    // 删除用户环境变量
                    Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除环境变量失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用UAC删除系统环境变量
        /// </summary>
        private bool DeleteSystemVariableWithUac(string name)
        {
            try
            {
                // 使用setx命令设置空值来删除系统环境变量
                var startInfo = new ProcessStartInfo
                {
                    FileName = "setx",
                    Arguments = $"\"{name}\" \"\" /M", // 设置为空值来删除
                    Verb = "runas", // 触发UAC
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // 用户取消了UAC提示
                Debug.WriteLine("用户取消了UAC提权");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UAC删除系统环境变量失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有用户环境变量
        /// </summary>
        public Dictionary<string, string> GetUserVariables()
        {
            var variables = new Dictionary<string, string>();
            try
            {
                var envVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
                foreach (System.Collections.DictionaryEntry entry in envVars)
                {
                    variables[entry.Key.ToString()] = entry.Value?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取用户环境变量失败: {ex.Message}");
            }
            return variables;
        }

        /// <summary>
        /// 获取所有系统环境变量
        /// </summary>
        public Dictionary<string, string> GetSystemVariables()
        {
            var variables = new Dictionary<string, string>();
            try
            {
                var envVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
                foreach (System.Collections.DictionaryEntry entry in envVars)
                {
                    variables[entry.Key.ToString()] = entry.Value?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取系统环境变量失败: {ex.Message}");
            }
            return variables;
        }

        /// <summary>
        /// 检查当前是否有管理员权限
        /// </summary>
        public bool HasAdminPrivileges()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 转义命令行参数
        /// </summary>
        private string EscapeArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return argument;

            // 转义引号和特殊字符
            return argument.Replace("\"", "\\\"");
        }

        /// <summary>
        /// 批量设置环境变量（用于导入）
        /// </summary>
        public async Task<bool> BatchSetVariablesAsync(List<SystemEnvironmentVariable> variables, bool isSystemVariables)
        {
            if (isSystemVariables && !HasAdminPrivileges())
            {
                return await Task.Run(() =>
                {
                    foreach (var variable in variables)
                    {
                        if (!SetSystemVariableWithUac(variable.Name, variable.Value))
                        {
                            return false;
                        }
                    }
                    return true;
                });
            }
            else
            {
                // 普通权限即可
                return await Task.Run(() =>
                {
                    var target = isSystemVariables ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
                    foreach (var variable in variables)
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(variable.Name, variable.Value, target);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"设置环境变量 {variable.Name} 失败: {ex.Message}");
                            return false;
                        }
                    }
                    return true;
                });
            }
        }
    }
}