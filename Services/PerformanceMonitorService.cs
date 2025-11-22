using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProjectManager.Models;
using ProjectManager.Helpers;
using System.Runtime.InteropServices;

namespace ProjectManager.Services
{
    public class PerformanceMonitorService : IPerformanceMonitorService
    {
        private readonly IProjectService _projectService;
        private readonly Dictionary<int, CpuSample> _cpuSamples = new();
        private readonly object _cpuLock = new();

        public PerformanceMonitorService(IProjectService projectService)
        {
            _projectService = projectService;
        }

        public async Task<IReadOnlyList<ProjectPerformanceSnapshot>> GetProjectPerformanceAsync(CancellationToken cancellationToken = default)
        {
            var projects = await _projectService.GetProjectsAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var snapshots = new List<ProjectPerformanceSnapshot>(projects.Count);
            var now = DateTime.Now;

            foreach (var project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshot = CreateSnapshot(project, now);
                snapshots.Add(snapshot);
            }

            return snapshots;
        }

        private ProjectPerformanceSnapshot CreateSnapshot(Project project, DateTime capturedAt)
        {
            Process? process = ResolveProcessForSnapshot(project);

            var snapshot = new ProjectPerformanceSnapshot
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                Status = project.Status,
                StatusDisplay = project.StatusDisplay,
                Framework = project.Framework,
                LocalPath = project.LocalPath,
                StartCommand = project.StartCommand,
                CapturedAt = capturedAt
            };

            if (process == null)
            {
                return snapshot;
            }

            snapshot.ProcessId = SafeGet<long?>(() => process.Id);
            snapshot.ProcessName = SafeGet(() => process.ProcessName);
            if (!TryPopulateAggregatedMemory(process, snapshot))
            {
                snapshot.MemoryUsageMb = SafeGet(() => process.WorkingSet64 / 1024d / 1024d);
                snapshot.PrivateMemoryUsageMb = SafeGet(() => process.PrivateMemorySize64 / 1024d / 1024d);
            }
            snapshot.ThreadCount = SafeGet(() => process.Threads.Count);
            snapshot.ProcessStartTime = SafeGet<DateTime?>(() => process.StartTime);
            snapshot.Uptime = snapshot.ProcessStartTime.HasValue ? capturedAt - snapshot.ProcessStartTime.Value : null;

            snapshot.CpuUsagePercent = CalculateCpuUsage(process);

            // 采集系统总物理内存（MB）
            snapshot.TotalMemoryMb = SafeGet(() => GetTotalPhysicalMemoryMb());

            return snapshot;
        }

        private double CalculateCpuUsage(Process process)
        {
            try
            {
                var now = DateTime.UtcNow;
                var totalProcessorTime = process.TotalProcessorTime;

                lock (_cpuLock)
                {
                    if (_cpuSamples.TryGetValue(process.Id, out var sample))
                    {
                        var cpuDelta = (totalProcessorTime - sample.TotalProcessorTime).TotalMilliseconds;
                        var timeDelta = (now - sample.Timestamp).TotalMilliseconds;
                        _cpuSamples[process.Id] = new CpuSample(totalProcessorTime, now);

                        if (timeDelta <= 0)
                            return 0d;

                        var usage = cpuDelta / (Environment.ProcessorCount * timeDelta) * 100d;
                        if (double.IsNaN(usage) || double.IsInfinity(usage))
                            return 0d;
                        return Math.Clamp(usage, 0d, 100d);
                    }

                    _cpuSamples[process.Id] = new CpuSample(totalProcessorTime, now);
                    return 0d;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"计算CPU使用率失败: {ex.Message}");
                RemoveCpuSample(process.Id);
                return 0d;
            }
        }

        private double GetTotalPhysicalMemoryMb()
        {
            try
            {
                var mem = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(mem))
                {
                    return mem.ullTotalPhys / 1024d / 1024d;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取总内存失败: {ex.Message}");
            }

            return 0d;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private Process? ResolveProcessForSnapshot(Project project)
        {
            Process? storedProcess = null;
            try
            {
                storedProcess = project.RunningProcess;
            }
            catch
            {
                storedProcess = null;
            }

            if (storedProcess == null)
                return null;

            Process? resolved = null;
            try
            {
                resolved = ProcessInterop.TryResolveRealProcess(storedProcess) ?? storedProcess;

                if (resolved == null)
                    return null;

                if (resolved.Id != storedProcess.Id)
                {
                    RemoveCpuSample(storedProcess.Id);
                }

                if (resolved.HasExited)
                {
                    RemoveCpuSample(resolved.Id);
                    return null;
                }
            }
            catch
            {
                return null;
            }

            return resolved;
        }

        private void RemoveCpuSample(int pid)
        {
            lock (_cpuLock)
            {
                _cpuSamples.Remove(pid);
            }
        }

        private bool TryPopulateAggregatedMemory(Process process, ProjectPerformanceSnapshot snapshot)
        {
            try
            {
                if (ProcessInterop.TryGetAggregatedMemoryUsage(process, out var workingMb, out var privateMb))
                {
                    snapshot.MemoryUsageMb = workingMb;
                    snapshot.PrivateMemoryUsageMb = privateMb;
                    return true;
                }
            }
            catch
            {
                // ignored, will fall back to single-process metrics
            }

            return false;
        }

        private static T SafeGet<T>(Func<T> accessor)
        {
            try
            {
                return accessor();
            }
            catch
            {
                return default!;
            }
        }

        private readonly record struct CpuSample(TimeSpan TotalProcessorTime, DateTime Timestamp);
    }
}
