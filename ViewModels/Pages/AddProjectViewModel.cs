using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Models;
using ProjectManager.Services;
using ProjectManager.ViewModels.Dialogs;
using ProjectManager.Views.Dialogs;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace ProjectManager.ViewModels.Pages
{
    public partial class AddProjectViewModel : ObservableObject, INavigationAware
    {
        private readonly IProjectService _projectService;
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;

        public AddProjectViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        private void CreateNewProject()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<ProjectEditDialogViewModel>();
            var window = _serviceProvider.GetRequiredService<ProjectEditWindow>();
            
            dialogViewModel.LoadProject();
            
            var result = window.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                // 导航到项目页面
                _navigationService.Navigate(typeof(Views.Pages.ProjectsPage));
            }
        }

        [RelayCommand]
        private void ImportExistingProject()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择项目文件夹",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                var projectPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(projectPath))
                {
                    var dialogViewModel = _serviceProvider.GetRequiredService<ProjectEditDialogViewModel>();
                    var window = _serviceProvider.GetRequiredService<ProjectEditWindow>();
                    
                    // 预填充项目信息
                    dialogViewModel.LoadProject();
                    dialogViewModel.ProjectName = Path.GetFileName(projectPath);
                    dialogViewModel.LocalPath = projectPath;
                    dialogViewModel.WorkingDirectory = projectPath;
                    
                    var result = window.ShowDialog(Application.Current.MainWindow);
                    if (result == true)
                    {
                        // 导航到项目页面
                        _navigationService.Navigate(typeof(Views.Pages.ProjectsPage));
                    }
                }
            }
        }

        public void OnNavigatedTo()
        {
        }

        public void OnNavigatedFrom()
        {
        }

        public async Task OnNavigatedToAsync()
        {
            await Task.CompletedTask;
        }

        public async Task OnNavigatedFromAsync()
        {
            await Task.CompletedTask;
        }
    }
}
