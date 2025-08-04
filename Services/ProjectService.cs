using System.Text.Json;
using System.Diagnostics;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetProjectsAsync();
        Task SaveProjectAsync(Project project);
        Task DeleteProjectAsync(string projectId);
        Task<Project?> GetProjectAsync(string projectId);
        Task StartProjectAsync(Project project);
        Task StopProjectAsync(Project project);
        Task<string> GetProjectLogsAsync(string projectId);
        event EventHandler<ProjectStatusChangedEventArgs>? ProjectStatusChanged;
    }

    public class ProjectService : IProjectService
    {
        private readonly string _projectsFilePath;
        private readonly List<Project> _projects;
        private readonly TerminalService _terminalService;
        private readonly IGitService _gitService;

        public event EventHandler<ProjectStatusChangedEventArgs>? ProjectStatusChanged;

        public ProjectService(TerminalService terminalService, IGitService gitService)
        {
            _terminalService = terminalService;
            _gitService = gitService;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ProjectManager");
            Directory.CreateDirectory(appFolder);
            _projectsFilePath = Path.Combine(appFolder, "projects.json");
            _projects = new List<Project>();
            LoadProjects();
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            // 加载Git信息
            foreach (var project in _projects)
            {
                try
                {
                    project.GitInfo = await _gitService.GetGitInfoAsync(project.LocalPath);
                }
                catch
                {
                    // 忽略Git信息加载错误
                }
            }

            return await Task.FromResult(_projects.ToList());
        }

        public async Task SaveProjectAsync(Project project)
        {
            // 检查项目名称是否已存在（不包括当前项目本身）
            var existingProjectWithSameName = _projects.FirstOrDefault(p => 
                p.Name.Equals(project.Name, StringComparison.OrdinalIgnoreCase) && p.Id != project.Id);
            
            if (existingProjectWithSameName != null)
            {
                throw new InvalidOperationException($"项目名称 '{project.Name}' 已存在，请使用不同的名称。");
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
                        project.StartCommand);
                }
                else
                {
                    // 使用现有会话，但更新启动命令和工作目录
                    terminalSession = existingSession;
                    terminalSession.Command = project.StartCommand;
                    terminalSession.ProjectPath = string.IsNullOrEmpty(project.WorkingDirectory) ? project.LocalPath : project.WorkingDirectory;
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
                    var projects = JsonSerializer.Deserialize<List<Project>>(json);
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
