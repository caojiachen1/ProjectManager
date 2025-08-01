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

        public AddProjectViewModel(IProjectService projectService, INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _projectService = projectService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        private async Task CreateNewProject()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<ProjectEditDialogViewModel>();
            var window = _serviceProvider.GetRequiredService<ProjectEditWindow>();
            
            dialogViewModel.LoadProject();
            
            var result = window.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                // 如果选择了框架且路径存在，询问是否创建项目模板
                if (!string.IsNullOrEmpty(dialogViewModel.Framework) && 
                    !string.IsNullOrEmpty(dialogViewModel.LocalPath) &&
                    Directory.Exists(dialogViewModel.LocalPath))
                {
                    var templateService = _serviceProvider.GetService<IProjectTemplateService>();
                    if (templateService != null)
                    {
                        var isEmpty = await templateService.IsValidProjectDirectoryAsync(dialogViewModel.LocalPath);
                        if (isEmpty)
                        {
                            var messageBox = new MessageBox
                            {
                                Title = "创建项目模板",
                                Content = $"是否为 {dialogViewModel.Framework} 项目创建基础代码模板？\n这将创建示例代码文件和配置文件。",
                                PrimaryButtonText = "创建模板",
                                SecondaryButtonText = "跳过"
                            };

                            var createTemplate = await messageBox.ShowDialogAsync();
                            if (createTemplate == MessageBoxResult.Primary)
                            {
                                try
                                {
                                    await templateService.CreateProjectTemplateAsync(
                                        dialogViewModel.LocalPath, 
                                        dialogViewModel.Framework, 
                                        dialogViewModel.ProjectName);
                                    
                                    var successBox = new MessageBox
                                    {
                                        Title = "成功",
                                        Content = "项目模板创建成功！",
                                        PrimaryButtonText = "确定"
                                    };
                                    await successBox.ShowDialogAsync();
                                }
                                catch (Exception ex)
                                {
                                    var errorBox = new MessageBox
                                    {
                                        Title = "错误",
                                        Content = $"创建项目模板失败：{ex.Message}",
                                        PrimaryButtonText = "确定"
                                    };
                                    await errorBox.ShowDialogAsync();
                                }
                            }
                        }
                    }
                }
                
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
                                
                                if (detectionResult.SuggestedPort > 0)
                                {
                                    dialogViewModel.Port = detectionResult.SuggestedPort.ToString();
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
            var dialog = _serviceProvider.GetRequiredService<GitCloneDialog>();
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 克隆成功，导航到项目页面
                _navigationService.Navigate(typeof(Views.Pages.ProjectsPage));
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