using ProjectManager.Models;

namespace ProjectManager.Services
{
    public interface IPerformanceMonitorService
    {
        Task<IReadOnlyList<ProjectPerformanceSnapshot>> GetProjectPerformanceAsync(CancellationToken cancellationToken = default);
    }
}
