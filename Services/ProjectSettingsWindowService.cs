using ProjectManager.Models;
using ProjectManager.Views.Dialogs;
using ProjectManager.ViewModels.Dialogs;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectManager.Services
{
    public class ProjectSettingsWindowService : IProjectSettingsWindowService
    {
        private readonly IServiceProvider _serviceProvider;

        public ProjectSettingsWindowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool? ShowSettingsWindow(Project project, Window owner)
        {
            return project.Framework switch
            {
                "ComfyUI" => ShowComfyUISettings(project, owner),
                "Node.js" => ShowNodeJSSettings(project, owner),
                ".NET" => ShowDotNetSettings(project, owner),
                _ => ShowGenericSettings(project, owner)
            };
        }

        private bool? ShowComfyUISettings(Project project, Window owner)
        {
            var viewModel = _serviceProvider.GetRequiredService<ComfyUIProjectSettingsViewModel>();
            var window = new ComfyUIProjectSettingsWindow(viewModel);
            
            viewModel.LoadProject(project);
            window.Owner = owner;
            
            return window.ShowDialog();
        }

        private bool? ShowNodeJSSettings(Project project, Window owner)
        {
            var viewModel = _serviceProvider.GetRequiredService<NodeJSProjectSettingsViewModel>();
            var window = new NodeJSProjectSettingsWindow(viewModel);
            
            viewModel.LoadProject(project);
            window.Owner = owner;
            
            return window.ShowDialog();
        }

        private bool? ShowDotNetSettings(Project project, Window owner)
        {
            var viewModel = _serviceProvider.GetRequiredService<DotNetProjectSettingsViewModel>();
            var window = new DotNetProjectSettingsWindow(viewModel);
            
            viewModel.LoadProject(project);
            window.Owner = owner;
            
            return window.ShowDialog();
        }

        private bool? ShowGenericSettings(Project project, Window owner)
        {
            var viewModel = _serviceProvider.GetRequiredService<GenericProjectSettingsViewModel>();
            var window = new GenericProjectSettingsWindow(viewModel);
            
            viewModel.LoadProject(project);
            window.Owner = owner;
            
            return window.ShowDialog();
        }
    }
}
