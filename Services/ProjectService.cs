using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Windows;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    public class ProjectService : IProjectService
    {
        private readonly string _projectsFilePath;
        private readonly List<Project> _projects;
        private readonly TerminalService _terminalService;
        private readonly IGitService _gitService;
        private readonly IErrorDisplayService _errorDisplayService;
        private static readonly TimeSpan GitInfoCacheDuration = TimeSpan.FromMinutes(2);
        private readonly ConcurrentDictionary<string, GitInfoCacheEntry> _gitInfoCache = new();
        private readonly ConcurrentDictionary<string, Task> _gitInfoRefreshTasks = new();

        public event EventHandler<ProjectStatusChangedEventArgs>? ProjectStatusChanged;

        public ProjectService(TerminalService terminalService, IGitService gitService, IErrorDisplayService errorDisplayService)
        {
            _terminalService = terminalService;
            _gitService = gitService;
            _errorDisplayService = errorDisplayService;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ProjectManager");
            Directory.CreateDirectory(appFolder);
            _projectsFilePath = Path.Combine(appFolder, "projects.json");
            _projects = new List<Project>();
            LoadProjects();
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            var snapshot = _projects.ToList();

            foreach (var project in snapshot)
            {
                if (!TryApplyCachedGitInfo(project))
                {
                    QueueGitInfoRefresh(project);
                }
            }

            return await Task.FromResult(snapshot);
        }

        public async Task<bool> SaveProjectAsync(Project project)
        {
            // 检查项目名称是否已存在（不包括当前项目本身）
            var existingProjectWithSameName = _projects.FirstOrDefault(p => 
                p.Name.Equals(project.Name, StringComparison.OrdinalIgnoreCase) && p.Id != project.Id);
            
            if (existingProjectWithSameName != null)
            {
                await _errorDisplayService.ShowWarningAsync(
                    $"项目名称 '{project.Name}' 已存在，请使用不同的名称。",
                    "项目名称冲突");
                return false;
            }

            var existingProject = _projects.FirstOrDefault(p => p.Id == project.Id);
            if (existingProject != null)
            {
                var index = _projects.IndexOf(existingProject);
                _projects[index] = project;
            }
            else
            {
                _projects.Add(project);
            }

            project.LastModified = DateTime.Now;
            await SaveProjectsToFile();
            InvalidateGitInfoCache(project.Id);
            QueueGitInfoRefresh(project);
            return true;
        }

        public async Task DeleteProjectAsync(string projectId)
        {
            var project = _projects.FirstOrDefault(p => p.Id == projectId);
            if (project != null)
            {
                if (project.Status == ProjectStatus.Running)
                {
                    await StopProjectAsync(project);
                }
                _projects.Remove(project);
                InvalidateGitInfoCache(project.Id);
                await SaveProjectsToFile();
            }
        }

        public async Task<Project?> GetProjectAsync(string projectId)
        {
            return await Task.FromResult(_projects.FirstOrDefault(p => p.Id == projectId));
        }

        public async Task StartProjectAsync(Project project)
        {
            if (project.Status == ProjectStatus.Running)
                return;

            try
            {
                project.Status = ProjectStatus.Starting;
                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));

                // 创建或获取终端会话
                var existingSessions = _terminalService.GetAllSessions();
                var existingSession = existingSessions.FirstOrDefault(s => s.ProjectName == project.Name);
                
                TerminalSession terminalSession;
                if (existingSession == null)
                {
                    // 创建新的终端会话
                    terminalSession = _terminalService.CreateSession(
                        project.Name, 
                        string.IsNullOrEmpty(project.WorkingDirectory) ? project.LocalPath : project.WorkingDirectory,
                        project.StartCommand,
                        project.EnvironmentVariables);
                }
                else
                {
                    // 使用现有会话，但更新启动命令和工作目录
                    terminalSession = existingSession;
                    terminalSession.Command = project.StartCommand;
                    terminalSession.ProjectPath = string.IsNullOrEmpty(project.WorkingDirectory) ? project.LocalPath : project.WorkingDirectory;
                }

                // 启动终端会话，传递环境变量
                // 若为 ComfyUI 项目，则自动注入 Python UTF-8（对其他项目不注入，不做任何提示）
                Dictionary<string, string>? envForLaunch = project.EnvironmentVariables;
                if (!string.IsNullOrWhiteSpace(project.Framework) && project.Framework.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase))
                {
                    envForLaunch = new Dictionary<string, string>(project.EnvironmentVariables ?? new Dictionary<string, string>())
                    {
                        ["PYTHONUTF8"] = "1",
                        ["PYTHONIOENCODING"] = "UTF-8"
                    };
                }
                var sessionStarted = await _terminalService.StartSessionAsync(terminalSession, envForLaunch);
                
                if (sessionStarted)
                {
                    // 如果终端会话启动成功，同步项目状态
                    project.RunningProcess = terminalSession.Process;
                    project.Status = ProjectStatus.Running;
                    
                    // 监听进程退出事件
                    if (terminalSession.Process != null)
                    {
                        terminalSession.Process.Exited += (sender, e) =>
                        {
                            project.Status = ProjectStatus.Stopped;
                            project.RunningProcess = null;
                            ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));
                        };
                    }
                }
                else
                {
                    project.Status = ProjectStatus.Error;
                    project.LogOutput += $"[{DateTime.Now:HH:mm:ss}] 终端会话启动失败\n";
                }

                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));
                _ = await SaveProjectAsync(project);
            }
            catch (Exception ex)
            {
                project.Status = ProjectStatus.Error;
                project.LogOutput += $"[{DateTime.Now:HH:mm:ss}] 启动失败: {ex.Message}\n";
                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));
                
                // 显示错误消息
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"项目启动失败: {ex.Message}", "项目启动错误"));
            }
        }

        public async Task StopProjectAsync(Project project)
        {
            if (project.Status != ProjectStatus.Running || project.RunningProcess == null)
                return;

            try
            {
                project.Status = ProjectStatus.Stopping;
                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));

                // 同时停止终端会话
                var existingSessions = _terminalService.GetAllSessions();
                var terminalSession = existingSessions.FirstOrDefault(s => s.ProjectName == project.Name);
                if (terminalSession != null)
                {
                    _terminalService.StopSession(terminalSession);
                }

                project.RunningProcess.Kill(true);
                project.RunningProcess = null;
                project.Status = ProjectStatus.Stopped;
                
                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));
                _ = await SaveProjectAsync(project);
            }
            catch (Exception ex)
            {
                project.Status = ProjectStatus.Error;
                project.LogOutput += $"[{DateTime.Now:HH:mm:ss}] 停止失败: {ex.Message}\n";
                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));
                
                // 显示错误消息
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"项目停止失败: {ex.Message}", "项目停止错误"));
            }
        }

        public async Task<string> GetProjectLogsAsync(string projectId)
        {
            var project = await GetProjectAsync(projectId);
            return project?.LogOutput ?? string.Empty;
        }

        public async Task SaveProjectsOrderAsync(IEnumerable<Project> orderedProjects)
        {
            if (orderedProjects == null) return;

            // Rebuild internal list order based on provided sequence of project IDs
            var orderedList = orderedProjects.Select(p => p.Id).ToList();
            var newList = new List<Project>();
            foreach (var id in orderedList)
            {
                var existing = _projects.FirstOrDefault(p => p.Id == id);
                if (existing != null)
                    newList.Add(existing);
            }

            // Append any missing projects that were not included in the ordered list
            foreach (var p in _projects)
            {
                if (!newList.Any(np => np.Id == p.Id))
                    newList.Add(p);
            }

            _projects.Clear();
            _projects.AddRange(newList);

            await SaveProjectsToFile();
        }

        private void LoadProjects()
        {
            try
            {
                if (!File.Exists(_projectsFilePath)) return;

                var json = File.ReadAllText(_projectsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var dtoList = JsonSerializer.Deserialize<List<PersistedProject>>(json, options);
                if (dtoList == null) return;

                foreach (var dto in dtoList)
                {
                    try
                    {
                        var model = ProjectPersistenceMapper.ToModel(dto);
                        _projects.Add(model);
                    }
                    catch (Exception mapEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"映射项目失败 (ID={dto.Id}): {mapEx.Message}");
                        // 显示映射错误
                        _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"项目映射失败: {mapEx.Message}", "项目加载错误"));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载项目失败: {ex.Message}");
                // 显示加载错误
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"加载项目失败: {ex.Message}", "项目加载错误"));
            }
        }

        private async Task SaveProjectsToFile()
        {
            try
            {
                var dtoList = _projects.Select(ProjectPersistenceMapper.ToDto).ToList();
                // 强制保存时排除运行中状态，避免误恢复
                foreach (var dto in dtoList)
                {
                    if (dto.Status == ProjectStatus.Running || dto.Status == ProjectStatus.Starting || dto.Status == ProjectStatus.Stopping)
                        dto.Status = ProjectStatus.Stopped;
                }
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(dtoList, options);
                await File.WriteAllTextAsync(_projectsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存项目失败: {ex.Message}");
                // 显示保存错误
                _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync($"保存项目失败: {ex.Message}", "项目保存错误"));
            }
        }
        private void QueueGitInfoRefresh(Project project)
        {
            if (string.IsNullOrWhiteSpace(project?.LocalPath))
                return;

            _gitInfoRefreshTasks.GetOrAdd(project.Id, _ => RefreshGitInfoCoreAsync(project));
        }

        private bool TryApplyCachedGitInfo(Project project)
        {
            if (_gitInfoCache.TryGetValue(project.Id, out var entry))
            {
                if (DateTime.UtcNow - entry.Timestamp <= GitInfoCacheDuration)
                {
                    project.GitInfo = entry.Info;
                    return true;
                }

                _gitInfoCache.TryRemove(project.Id, out _);
            }

            return false;
        }

        private async Task RefreshGitInfoCoreAsync(Project project)
        {
            try
            {
                var gitInfo = await _gitService.GetGitInfoAsync(project.LocalPath);
                _gitInfoCache[project.Id] = new GitInfoCacheEntry(gitInfo, DateTime.UtcNow);
                await RunOnUiThreadAsync(() => project.GitInfo = gitInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新Git信息失败: {ex.Message}");
            }
            finally
            {
                _gitInfoRefreshTasks.TryRemove(project.Id, out _);
            }
        }

        private void InvalidateGitInfoCache(string projectId)
        {
            _gitInfoCache.TryRemove(projectId, out _);
            _gitInfoRefreshTasks.TryRemove(projectId, out _);
        }

        private static Task RunOnUiThreadAsync(Action updateAction)
        {
            if (updateAction == null)
                return Task.CompletedTask;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                updateAction();
                return Task.CompletedTask;
            }

            return dispatcher.InvokeAsync(updateAction).Task;
        }

        private readonly record struct GitInfoCacheEntry(GitInfo Info, DateTime Timestamp);
    }
}
