using ProjectManager.Models;
using System.Windows;

namespace ProjectManager.Services
{
    public interface IProjectSettingsWindowService
    {
        bool? ShowSettingsWindow(Project project, Window owner);
    }
}
