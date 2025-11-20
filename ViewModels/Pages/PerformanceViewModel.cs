using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Models;
using ProjectManager.Services;
using Wpf.Ui.Abstractions.Controls;

namespace ProjectManager.ViewModels.Pages
{
    public partial class PerformanceViewModel : ObservableObject, INavigationAware
    {
        private readonly IPerformanceMonitorService _performanceMonitorService;
        private readonly IErrorDisplayService _errorDisplayService;
        private CancellationTokenSource? _monitoringCts;

        private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(1);

        [ObservableProperty]
        private ObservableCollection<ProjectPerformanceSnapshot> _projectMetrics = new();

        [ObservableProperty]
        private DateTime _lastUpdated = DateTime.MinValue;

        [ObservableProperty]
        private bool _isMonitoringActive;

        [ObservableProperty]
        private string _statusMessage = "等待监控启动...";

        [ObservableProperty]
        private double _totalCpuUsage;

        [ObservableProperty]
        private double? _totalGpuUsage;

        [ObservableProperty]
        private double _totalMemoryUsageMb;

        [ObservableProperty]
        private int _runningProjects;

        [ObservableProperty]
        private int _totalProjects;

        [ObservableProperty]
        private bool _hasGpuData;

        public PerformanceViewModel(IPerformanceMonitorService performanceMonitorService, IErrorDisplayService errorDisplayService)
        {
            _performanceMonitorService = performanceMonitorService;
            _errorDisplayService = errorDisplayService;
        }

        public void OnNavigatedTo()
        {
            StartMonitoringLoop();
        }

        public void OnNavigatedFrom()
        {
            StopMonitoringLoop();
        }

        public Task OnNavigatedToAsync()
        {
            StartMonitoringLoop();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            StopMonitoringLoop();
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task RefreshNow()
        {
            try
            {
                await RefreshMetricsAsync(_monitoringCts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Ignore if cancelled by navigation
            }
            catch (Exception ex)
            {
                await _errorDisplayService.ShowErrorAsync($"刷新性能数据失败: {ex.Message}", "性能监控错误");
            }
        }

        [RelayCommand]
        private void OpenInExplorer(ProjectPerformanceSnapshot? snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.LocalPath) || !Directory.Exists(snapshot.LocalPath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = snapshot.LocalPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                _ = _errorDisplayService.ShowErrorAsync($"无法打开资源管理器: {ex.Message}", "打开路径失败");
            }
        }

        private void StartMonitoringLoop()
        {
            if (_monitoringCts != null)
                return;

            _monitoringCts = new CancellationTokenSource();
            _ = MonitorLoopAsync(_monitoringCts.Token);
        }

        private void StopMonitoringLoop()
        {
            if (_monitoringCts == null)
                return;

            _monitoringCts.Cancel();
            _monitoringCts.Dispose();
            _monitoringCts = null;
            IsMonitoringActive = false;
            StatusMessage = "监控已暂停";
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            IsMonitoringActive = true;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RefreshMetricsAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _errorDisplayService.ShowErrorAsync($"性能监控失败: {ex.Message}", "性能监控错误");
                }

                try
                {
                    await Task.Delay(_refreshInterval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            IsMonitoringActive = false;
        }

        private async Task RefreshMetricsAsync(CancellationToken token)
        {
            var snapshots = await _performanceMonitorService.GetProjectPerformanceAsync(token);
            token.ThrowIfCancellationRequested();

            var ordered = snapshots
                .OrderByDescending(s => s.IsRunning)
                .ThenByDescending(s => s.CpuUsagePercent)
                .ThenBy(s => s.ProjectName)
                .ToList();

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                ApplySnapshots(ordered);
                return;
            }

            await dispatcher.InvokeAsync(() => ApplySnapshots(ordered), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ApplySnapshots(IList<ProjectPerformanceSnapshot> ordered)
        {
            ProjectMetrics.Clear();
            // Normalize GPU values: some sources may provide 0..1 fractions, others 0..100 percentages.
            foreach (var snapshot in ordered)
            {
                if (snapshot.GpuUsagePercent.HasValue)
                {
                    var v = snapshot.GpuUsagePercent.Value;
                    // if value looks like a fraction (<= 1), treat as 0..1 and convert to percent
                    if (v <= 1d)
                        v = v * 100d;

                    // clamp to 0..100
                    v = Math.Clamp(v, 0d, 100d);
                    snapshot.GpuUsagePercent = Math.Round(v, 1);
                }

                ProjectMetrics.Add(snapshot);
            }

            TotalProjects = ordered.Count;
            RunningProjects = ordered.Count(s => s.IsRunning);
            TotalCpuUsage = Math.Round(ordered.Where(s => s.IsRunning).Sum(s => s.CpuUsagePercent), 1);
            var gpuSum = ordered.Where(s => s.GpuUsagePercent.HasValue).Sum(s => s.GpuUsagePercent!.Value);
            // Always provide a numeric total (0.0 when no GPU data) so header TextBlock with StringFormat can display a percentage sign.
            TotalGpuUsage = Math.Round(gpuSum, 1);
            HasGpuData = ordered.Any(s => s.GpuUsagePercent.HasValue) && gpuSum > 0;

            // Total memory usage (MB) across running projects
            var totalMem = ordered.Where(s => s.IsRunning).Sum(s => s.MemoryUsageMb);
            TotalMemoryUsageMb = Math.Round(totalMem, 1);
            LastUpdated = DateTime.Now;
            StatusMessage = RunningProjects > 0
                ? $"监控中 · {RunningProjects}/{TotalProjects} 个项目"
                : "暂无运行中的项目";
        }
    }
}
