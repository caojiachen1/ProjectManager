using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;

namespace ProjectManager.Helpers
{
    public static class UacHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
        private const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;
        private const int SW_SHOWNORMAL = 1;

        /// <summary>
        /// 检查当前进程是否以管理员权限运行
        /// </summary>
        public static bool IsRunAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 以管理员权限重新启动当前程序
        /// </summary>
        public static bool RestartAsAdmin(string arguments = "")
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule?.FileName,
                    Arguments = arguments,
                    Verb = "runas", // 触发UAC提权
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UAC提权失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 以管理员权限执行指定程序
        /// </summary>
        public static bool RunAsAdmin(string fileName, string arguments = "")
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    Verb = "runas",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UAC提权执行失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行需要管理员权限的操作，如果当前没有权限则请求提权
        /// </summary>
        public static bool ExecuteWithUac(Action adminAction, string arguments = "")
        {
            if (IsRunAsAdmin())
            {
                // 当前已有管理员权限，直接执行
                adminAction?.Invoke();
                return true;
            }
            else
            {
                // 没有管理员权限，需要提权后重新执行
                MessageBoxResult result = MessageBox.Show(
                    "此操作需要管理员权限。是否以管理员身份重新启动程序？",
                    "需要管理员权限",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 保存当前状态，以便重启后恢复
                    // Properties.Settings.Default.RestoreArguments = arguments;
                    // Properties.Settings.Default.Save();

                    if (RestartAsAdmin(arguments))
                    {
                        // 关闭当前实例
                        Application.Current.Shutdown();
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 静默提权（不显示确认对话框）
        /// </summary>
        public static bool ExecuteWithUacSilent(Action adminAction, string arguments = "")
        {
            if (IsRunAsAdmin())
            {
                adminAction?.Invoke();
                return true;
            }
            else
            {
                try
                {
                    // 保存状态并静默提权
                    // Properties.Settings.Default.RestoreArguments = arguments;
                    // Properties.Settings.Default.Save();

                    if (RestartAsAdmin(arguments))
                    {
                        Application.Current.Shutdown();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"静默UAC提权失败: {ex.Message}");
                }
                return false;
            }
        }
    }
}