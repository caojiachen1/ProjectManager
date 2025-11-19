using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Models;
using ProjectManager.Services;

namespace ProjectManager.ViewModels.Dialogs
{
    /// <summary>
    /// ComfyUI 插件管理窗口的视图模型。
    /// </summary>
    public partial class ComfyUIPluginsManagerViewModel : ObservableObject
    {
        private readonly IGitService _gitService;
        private readonly IErrorDisplayService _errorService;

        // 缓存每个插件目录的 Git 信息，避免重复执行 git 命令
        private readonly Dictionary<string, CachedGitInfo> _gitCache = new(StringComparer.OrdinalIgnoreCase);

        private sealed class CachedGitInfo
        {
            public bool IsGitRepository { get; init; }
            public string RemoteUrl { get; init; } = string.Empty;
            public string Branch { get; init; } = string.Empty;
            public string VersionId { get; init; } = string.Empty;
            public string LastCommitMessage { get; init; } = string.Empty;
        }

        [ObservableProperty]
        private string _customNodesPath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ComfyUIPluginInfo> _plugins = new();

        public ComfyUIPluginsManagerViewModel(IGitService gitService, IErrorDisplayService errorService)
        {
            _gitService = gitService;
            _errorService = errorService;
        }

        /// <summary>
        /// 非阻塞地启动插件加载（会在后台逐项填充 <see cref="Plugins"/>）。
        /// </summary>
        public void StartLoadFromCustomNodes(string customNodesPath)
        {
            // Fire-and-forget: 不 await 以避免阻塞 UI
            _ = LoadFromCustomNodesAsync(customNodesPath);
        }

        public async Task LoadFromCustomNodesAsync(string customNodesPath)
        {
            // 路径变化时清空缓存，避免不同项目之间干扰；同一路径下多次刷新则复用缓存
            if (!string.Equals(CustomNodesPath, customNodesPath, StringComparison.OrdinalIgnoreCase))
            {
                _gitCache.Clear();
            }

            CustomNodesPath = customNodesPath;

            // 确保 Plugins 集合存在，清空旧数据
            if (Plugins == null)
            {
                Plugins = new ObservableCollection<ComfyUIPluginInfo>();
            }
            else
            {
                // 在 UI 线程清空现有集合
                System.Windows.Application.Current.Dispatcher.Invoke(() => Plugins.Clear());
            }

            if (string.IsNullOrWhiteSpace(customNodesPath) || !Directory.Exists(customNodesPath))
            {
                return;
            }

            // 完全在后台线程执行目录扫描和插件创建，边扫描边添加到 UI
            await Task.Run(() =>
            {
                try
                {
                    var dirs = Directory.GetDirectories(customNodesPath);
                    
                    foreach (var dir in dirs)
                    {
                        try
                        {
                            var info = new DirectoryInfo(dir);

                            // 跳过 Python 缓存目录，例如 "__pycache__"
                            if (info.Name.Equals("__pycache__", StringComparison.OrdinalIgnoreCase)
                                || info.Name.IndexOf("pycache", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                continue;
                            }

                            var plugin = new ComfyUIPluginInfo
                            {
                                Name = info.Name,
                                LastUpdated = info.LastWriteTime,
                            };
                            // 保存插件目录的完整路径，便于后续删除等操作
                            plugin.Path = info.FullName;

                            // 如果缓存中已有 Git 信息，立即应用
                            if (_gitCache.TryGetValue(info.FullName, out var cached))
                            {
                                ApplyCachedGitInfo(plugin, cached);
                            }

                            // 必须在 UI 线程添加到可观察集合
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(() => Plugins.Add(plugin));

                            // 后台异步刷新 Git 信息（不等待）
                            _ = LoadGitInfoAsync(plugin, info.FullName);
                        }
                        catch
                        {
                            // 单个插件解析失败时忽略
                        }
                    }
                }
                catch
                {
                    // 目录访问失败时忽略
                }
            });
        }

        private async Task LoadGitInfoAsync(ComfyUIPluginInfo plugin, string directoryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                {
                    ApplyCachedGitInfo(plugin, new CachedGitInfo
                    {
                        IsGitRepository = false,
                        RemoteUrl = "不是git仓库"
                    });
                    return;
                }

                // 1. 如果有缓存，先把缓存显示出来，然后继续往下刷新为最新状态
                if (_gitCache.TryGetValue(directoryPath, out var cachedFromCache))
                {
                    ApplyCachedGitInfo(plugin, cachedFromCache);
                }

                // 2. 快速判断是否存在 .git 目录
                var isGitRepo = await _gitService.IsValidGitRepositoryAsync(directoryPath);
                if (!isGitRepo)
                {
                    var cached = new CachedGitInfo
                    {
                        IsGitRepository = false,
                        RemoteUrl = "不是git仓库"
                    };

                    _gitCache[directoryPath] = cached;
                    ApplyCachedGitInfo(plugin, cached);
                    return;
                }

                // 3. 是 Git 仓库，则获取详细 Git 信息
                var gitInfo = await _gitService.GetGitInfoAsync(directoryPath);

                if (!gitInfo.IsGitRepository)
                {
                    var cached = new CachedGitInfo
                    {
                        IsGitRepository = false,
                        RemoteUrl = "不是git仓库"
                    };

                    _gitCache[directoryPath] = cached;
                    ApplyCachedGitInfo(plugin, cached);
                    return;
                }

                var remoteUrl = string.IsNullOrWhiteSpace(gitInfo.RemoteUrl)
                    ? "(无远端)"
                    : gitInfo.RemoteUrl;

                var branch = gitInfo.CurrentBranch;
                var shortHash = await _gitService.GetShortCommitHashAsync(directoryPath);
                var lastMessage = gitInfo.LastCommitMessage;

                var updatedCache = new CachedGitInfo
                {
                    IsGitRepository = true,
                    RemoteUrl = remoteUrl,
                    Branch = branch,
                    VersionId = shortHash,
                    LastCommitMessage = lastMessage
                };

                _gitCache[directoryPath] = updatedCache;
                ApplyCachedGitInfo(plugin, updatedCache);
            }
            catch
            {
                var cached = new CachedGitInfo
                {
                    IsGitRepository = false,
                    RemoteUrl = "不是git仓库"
                };

                _gitCache[directoryPath] = cached;
                ApplyCachedGitInfo(plugin, cached);
            }
        }

        private static void ApplyCachedGitInfo(ComfyUIPluginInfo plugin, CachedGitInfo cached)
        {
            plugin.RemoteUrl = string.IsNullOrEmpty(cached.RemoteUrl) ? "" : cached.RemoteUrl;
            plugin.Branch = cached.Branch;
            plugin.VersionId = cached.VersionId;
            plugin.LastCommitMessage = cached.LastCommitMessage;
        }

        [RelayCommand]
        private Task Refresh()
        {
            if (!string.IsNullOrWhiteSpace(CustomNodesPath))
            {
                StartLoadFromCustomNodes(CustomNodesPath);
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task Remove(ComfyUIPluginInfo plugin)
        {
            if (plugin == null)
                return;

            // 确认删除
            var confirm = await _errorService.ShowConfirmationAsync($"确定要删除插件 '{plugin.Name}' 吗？此操作不可撤销。", "确认删除");
            if (!confirm)
                return;

            try
            {
                if (!string.IsNullOrWhiteSpace(plugin.Path) && Directory.Exists(plugin.Path))
                {
                    // 尝试删除目录及其内容
                    Directory.Delete(plugin.Path, true);
                }

                // 从集合中移除（在 UI 线程）
                System.Windows.Application.Current.Dispatcher.Invoke(() => Plugins?.Remove(plugin));
            }
            catch (Exception ex)
            {
                await _errorService.ShowExceptionAsync(ex, "删除失败");
            }
        }
    }
}
