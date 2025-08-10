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
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ProjectManager.ViewModels.Pages
{
    public partial class AddProjectViewModel : ObservableObject, INavigationAware
    {
        private readonly IProjectService _projectService;
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IErrorDisplayService _errorDisplayService;

        public AddProjectViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider, IErrorDisplayService errorDisplayService)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _errorDisplayService = errorDisplayService;
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
        private async Task CloneFromGit()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<GitCloneDialogViewModel>();
            var window = _serviceProvider.GetRequiredService<Views.Dialogs.GitCloneWindow>();
            
            window.Owner = Application.Current.MainWindow;
            var result = window.ShowDialog();
            if (result == true)
            {
                // 克隆成功，导航到项目页面
                _navigationService.Navigate(typeof(Views.Pages.ProjectsPage));
            }
            
            await Task.CompletedTask;
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