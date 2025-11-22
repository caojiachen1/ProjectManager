using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProjectManager.Helpers
{
    internal static class ProcessInterop
    {
        private static readonly HashSet<string> ShellProcessNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "cmd",
            "cmd.exe",
            "powershell",
            "powershell.exe",
            "pwsh",
            "pwsh.exe",
            "conhost",
            "conhost.exe",
            "bash",
            "bash.exe",
            "sh",
            "sh.exe",
            "wsl",
            "wsl.exe",
            "wt",
            "wt.exe",
            "git-bash",
            "git-bash.exe",
            "wezterm",
            "wezterm.exe"
        };

        public static Process? TryResolveRealProcess(Process? candidate)
        {
            if (candidate == null)
                return null;

            try
            {
                if (!candidate.HasExited && !IsShellProcess(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                return null;
            }

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            if (!TryEnqueue(candidate.Id))
            {
                return null;
            }

            while (queue.Count > 0)
            {
                var pid = queue.Dequeue();
                foreach (var childPid in EnumerateChildProcessIds(pid))
                {
                    if (!TryEnqueue(childPid))
                        continue;

                    try
                    {
                        var child = Process.GetProcessById(childPid);
                        if (child.HasExited)
                            continue;

                        if (!IsShellProcess(child))
                            return child;

                        queue.Enqueue(child.Id);
                    }
                    catch
                    {
                        // Ignore processes we cannot access
                    }
                }
            }

            try
            {
                return candidate.HasExited ? null : candidate;
            }
            catch
            {
                return null;
            }

            bool TryEnqueue(int pid)
            {
                if (pid <= 0)
                    return false;
                if (visited.Add(pid))
                {
                    queue.Enqueue(pid);
                    return true;
                }
                return false;
            }
        }

        public static bool TryGetAggregatedMemoryUsage(Process root, out double workingSetMb, out double privateMemoryMb, bool includeShellDescendants = false)
        {
            workingSetMb = 0;
            privateMemoryMb = 0;

            try
            {
                var visited = new HashSet<int>();
                var queue = new Queue<int>();

                void Enqueue(int pid)
                {
                    if (pid <= 0)
                        return;
                    if (visited.Add(pid))
                    {
                        queue.Enqueue(pid);
                    }
                }

                Enqueue(root.Id);

                while (queue.Count > 0)
                {
                    var pid = queue.Dequeue();
                    Process? proc = null;
                    try
                    {
                        proc = Process.GetProcessById(pid);
                    }
                    catch
                    {
                        continue;
                    }

                    using (proc)
                    {
                        var isShell = IsShellProcess(proc);
                        var shouldInclude = pid == root.Id || !isShell || includeShellDescendants;
                        if (shouldInclude)
                        {
                            try
                            {
                                workingSetMb += BytesToMegabytes(proc.WorkingSet64);
                                privateMemoryMb += BytesToMegabytes(proc.PrivateMemorySize64);
                            }
                            catch
                            {
                                // ignore per-process access errors but continue traversing children
                            }
                        }

                        foreach (var childPid in EnumerateChildProcessIds(proc.Id))
                        {
                            Enqueue(childPid);
                        }
                    }
                }

                return workingSetMb > 0 || privateMemoryMb > 0;
            }
            catch
            {
                workingSetMb = 0;
                privateMemoryMb = 0;
                return false;
            }
        }

        private static double BytesToMegabytes(long bytes) => bytes / 1024d / 1024d;

        public static IReadOnlyList<int> EnumerateChildProcessIds(int parentPid)
        {
            var list = new List<int>();
            if (parentPid <= 0)
                return list;

            var snapshot = CreateToolhelp32Snapshot(SnapshotFlags.Process, 0u);
            if (snapshot == IntPtr.Zero || snapshot == INVALID_HANDLE_VALUE)
                return list;

            try
            {
                var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
                if (Process32First(snapshot, ref entry))
                {
                    do
                    {
                        if ((int)entry.th32ParentProcessID == parentPid)
                        {
                            list.Add((int)entry.th32ProcessID);
                        }
                    } while (Process32Next(snapshot, ref entry));
                }
            }
            finally
            {
                CloseHandle(snapshot);
            }

            return list;
        }

        public static bool IsShellProcess(Process? process)
        {
            if (process == null)
                return false;

            try
            {
                return IsShellProcessName(process.ProcessName);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsShellProcessName(string? processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            var nameOnly = Path.GetFileName(processName);
            return ShellProcessNames.Contains(nameOnly ?? processName);
        }

        private const int MAX_PATH = 260;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

        [Flags]
        private enum SnapshotFlags : uint
        {
            Process = 0x00000002,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
