using ProjectManager.Models;

namespace ProjectManager.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetProjectsAsync();
        Task<bool> SaveProjectAsync(Project project);
        Task SaveProjectsOrderAsync(IEnumerable<Project> orderedProjects);
        Task DeleteProjectAsync(string projectId);
        Task<Project?> GetProjectAsync(string projectId);
        Task StartProjectAsync(Project project);
        Task StopProjectAsync(Project project);
        Task<string> GetProjectLogsAsync(string projectId);
        event EventHandler<ProjectStatusChangedEventArgs>? ProjectStatusChanged;
    }
}
