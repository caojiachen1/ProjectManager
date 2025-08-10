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
        private async Task ImportExistingProject()
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
                    
                    // 自动检测框架类型（使用增强检测）
                    try
                    {
                        var detectionService = _serviceProvider.GetService<IProjectDetectionService>();
                        if (detectionService != null)
                        {
                            var detectionResult = await detectionService.DetectProjectTypeAsync(projectPath);
                            if (detectionResult.ConfidenceLevel > 0.3 && detectionResult.DetectedFramework != "其他")
                            {
                                dialogViewModel.Framework = detectionResult.DetectedFramework;
                                
                                // 填充其他建议信息
                                if (!string.IsNullOrEmpty(detectionResult.SuggestedDescription))
                                {
                                    dialogViewModel.ProjectDescription = detectionResult.SuggestedDescription;
                                }
                                
                                if (!string.IsNullOrEmpty(detectionResult.SuggestedStartCommand))
                                {
                                    dialogViewModel.StartCommand = detectionResult.SuggestedStartCommand;
                                }
                                
                                if (detectionResult.SuggestedTags.Any())
                                {
                                    dialogViewModel.TagsString = string.Join(", ", detectionResult.SuggestedTags);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 如果增强检测失败，回退到原有检测
                        var detectedFramework = FrameworkConfigService.DetectFramework(projectPath);
                        if (detectedFramework != "其他")
                        {
                            dialogViewModel.Framework = detectedFramework;
                        }
                    }
                    
                    var result = window.ShowDialog(Application.Current.MainWindow);
                    if (result == true)
                    {
                        // 导航到项目页面
                        _navigationService.Navigate(typeof(Views.Pages.ProjectsPage));
                    }
                }
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