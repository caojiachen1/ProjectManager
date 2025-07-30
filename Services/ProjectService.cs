using System.Text.Json;
using System.Diagnostics;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    public interface IProjectService
    {
        Task<List<AiProject>> GetProjectsAsync();
        Task SaveProjectAsync(AiProject project);
        Task DeleteProjectAsync(string projectId);
        Task<AiProject?> GetProjectAsync(string projectId);
        Task StartProjectAsync(AiProject project);
        Task StopProjectAsync(AiProject project);
        Task<string> GetProjectLogsAsync(string projectId);
        event EventHandler<ProjectStatusChangedEventArgs>? ProjectStatusChanged;
    }

    public class ProjectService : IProjectService
    {
        private readonly string _projectsFilePath;
        private readonly List<AiProject> _projects;
        private readonly TerminalService _terminalService;

        public event EventHandler<ProjectStatusChangedEventArgs>? ProjectStatusChanged;

        public ProjectService(TerminalService terminalService)
        {
            _terminalService = terminalService;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "AI Project Manager");
            Directory.CreateDirectory(appFolder);
            _projectsFilePath = Path.Combine(appFolder, "projects.json");
            _projects = new List<AiProject>();
            LoadProjects();
        }

        public async Task<List<AiProject>> GetProjectsAsync()
        {
            return await Task.FromResult(_projects.ToList());
        }

        public async Task SaveProjectAsync(AiProject project)
        {
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
                await SaveProjectsToFile();
            }
        }

        public async Task<AiProject?> GetProjectAsync(string projectId)
        {
            return await Task.FromResult(_projects.FirstOrDefault(p => p.Id == projectId));
        }

        public async Task StartProjectAsync(AiProject project)
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
                        project.StartCommand);
                }
                else
                {
                    terminalSession = existingSession;
                }

                // 启动终端会话
                var sessionStarted = await _terminalService.StartSessionAsync(terminalSession);
                
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
                await SaveProjectAsync(project);
            }
            catch (Exception ex)
            {
                project.Status = ProjectStatus.Error;
                project.LogOutput += $"[{DateTime.Now:HH:mm:ss}] 启动失败: {ex.Message}\n";
                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));
            }
        }

        public async Task StopProjectAsync(AiProject project)
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
                await SaveProjectAsync(project);
            }
            catch (Exception ex)
            {
                project.Status = ProjectStatus.Error;
                project.LogOutput += $"[{DateTime.Now:HH:mm:ss}] 停止失败: {ex.Message}\n";
                ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project.Id, project.Status));
            }
        }

        public async Task<string> GetProjectLogsAsync(string projectId)
        {
            var project = await GetProjectAsync(projectId);
            return project?.LogOutput ?? string.Empty;
        }

        private void LoadProjects()
        {
            try
            {
                if (File.Exists(_projectsFilePath))
                {
                    var json = File.ReadAllText(_projectsFilePath);
                    var projects = JsonSerializer.Deserialize<List<AiProject>>(json);
                    if (projects != null)
                    {
                        _projects.AddRange(projects);
                        // 重置运行状态，因为应用重启后进程会丢失
                        foreach (var project in _projects.Where(p => p.Status == ProjectStatus.Running))
                        {
                            project.Status = ProjectStatus.Stopped;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不阻止应用启动
                System.Diagnostics.Debug.WriteLine($"加载项目失败: {ex.Message}");
            }
        }

        private async Task SaveProjectsToFile()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(_projects, options);
                await File.WriteAllTextAsync(_projectsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存项目失败: {ex.Message}");
            }
        }
    }

    public class ProjectStatusChangedEventArgs : EventArgs
    {
        public string ProjectId { get; }
        public ProjectStatus Status { get; }

        public ProjectStatusChangedEventArgs(string projectId, ProjectStatus status)
        {
            ProjectId = projectId;
            Status = status;
        }
    }
}
