using System.IO;
using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ProjectManager.Models;
using ProjectManager.Services;
using ProjectManager.ViewModels.Pages;
using ProjectManager.Views.Dialogs;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class GitCloneDialogViewModel : ObservableObject
    {
        private readonly IGitService _gitService;
        private readonly IProjectService _projectService;
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;
        private readonly IErrorDisplayService _errorDisplayService;

        public event EventHandler<bool>? CloneCompleted;

        [ObservableProperty]
        private string _repositoryUrl = string.Empty;

        [ObservableProperty]
        private string _localPath = string.Empty;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private bool _autoAddToManager = true;

        [ObservableProperty]
        private string _selectedFramework = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availableFrameworks = new();

        public bool AddToProjectManager 
        { 
            get => AutoAddToManager; 
            set => AutoAddToManager = value; 
        }

        [ObservableProperty]
        private bool _isCloning = false;

        [ObservableProperty]
        private string _cloneProgress = string.Empty;

        [ObservableProperty]
        private string _cloneStatusMessage = string.Empty;

        [ObservableProperty]
        private string _cloneLog = string.Empty;

        [ObservableProperty]
        private int _cloneProgressValue = 0;

        [ObservableProperty]
        private bool _hasUrlValidationError = false;

        [ObservableProperty]
        private string _urlValidationMessage = string.Empty;

        [ObservableProperty]
        private bool _isUrlValid = true;

        public bool CanClone => !string.IsNullOrWhiteSpace(RepositoryUrl) && 
                               !string.IsNullOrWhiteSpace(LocalPath) && 
                               IsUrlValid && 
                               !IsCloning;

        [RelayCommand]
        private async Task Clone()
        {
            if (!CanClone) return;
            var result = await CloneRepositoryAsync();
            CloneCompleted?.Invoke(this, result);
        }

        public GitCloneDialogViewModel(
            IGitService gitService, 
            IProjectService projectService,
            IServiceProvider serviceProvider,
            INavigationService navigationService,
            IErrorDisplayService errorDisplayService)
        {
            _gitService = gitService;
            _projectService = projectService;
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;
            _errorDisplayService = errorDisplayService;
            
            // 初始化可用框架列表
            AvailableFrameworks = new ObservableCollection<string>(FrameworkConfigService.GetFrameworkNames());
        }

        partial void OnRepositoryUrlChanged(string value)
        {
            OnPropertyChanged(nameof(CanClone));
            CloneCommand.NotifyCanExecuteChanged();
            _ = ValidateRepositoryUrlAsync();
            _ = ExtractProjectNameAsync();
        }

        partial void OnLocalPathChanged(string value)
        {
            OnPropertyChanged(nameof(CanClone));
            CloneCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsUrlValidChanged(bool value)
        {
            HasUrlValidationError = !value;
            OnPropertyChanged(nameof(CanClone));
            CloneCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsCloningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanClone));
            CloneCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void BrowseLocalPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择项目存储路径"
            };

            if (!string.IsNullOrEmpty(LocalPath) && Directory.Exists(LocalPath))
            {
                dialog.InitialDirectory = LocalPath;
            }

            if (dialog.ShowDialog() == true)
            {
                LocalPath = dialog.FolderName;
                
                // 如果有项目名称，自动添加到路径中
                if (!string.IsNullOrEmpty(ProjectName))
                {
                    LocalPath = Path.Combine(LocalPath, ProjectName);
                }
            }
        }

        public async Task<bool> CloneRepositoryAsync()
        {
            if (!CanClone)
                return false;

            try
            {
                IsCloning = true;
                CloneProgress = "准备克隆仓库...\n";
                CloneLog = "准备克隆仓库...\n";
                CloneStatusMessage = "准备克隆仓库...";

                // 确保本地路径包含项目名称
                var finalLocalPath = LocalPath;
                if (!string.IsNullOrEmpty(ProjectName) && !LocalPath.EndsWith(ProjectName))
                {
                    finalLocalPath = Path.Combine(LocalPath, ProjectName);
                }

                var progress = new Progress<string>(message =>
                {
                    CloneProgress += message + "\n";
                    CloneLog += message + "\n";
                    CloneStatusMessage = message;
                    
                    // 简单的进度估算
                    if (message.Contains("准备"))
                        CloneProgressValue = 10;
                    else if (message.Contains("克隆") || message.Contains("下载"))
                        CloneProgressValue = 50;
                    else if (message.Contains("成功") || message.Contains("完成"))
                        CloneProgressValue = 100;
                });

                var result = await _gitService.CloneRepositoryAsync(RepositoryUrl, finalLocalPath, progress);

                if (result.IsSuccess)
                {
                    CloneProgress += "克隆成功！\n";
                    CloneLog += "克隆成功！\n";
                    CloneStatusMessage = "克隆成功！";

                    if (AddToProjectManager)
                    {
                        CloneProgress += "正在添加到项目管理器...\n";
                        CloneLog += "正在添加到项目管理器...\n";
                        CloneStatusMessage = "正在添加到项目管理器...";
                        await AddClonedProjectToManagerAsync(finalLocalPath);
                    }

                    CloneProgress += "完成！\n";
                    CloneLog += "完成！\n";
                    CloneStatusMessage = "完成！";
                    return true;
                }
                else
                {
                    CloneProgress += $"克隆失败: {result.ErrorMessage}\n";
                    CloneLog += $"克隆失败: {result.ErrorMessage}\n";
                    CloneStatusMessage = $"克隆失败: {result.ErrorMessage}";
                    
                    await _errorDisplayService.ShowErrorAsync(result.ErrorMessage, "克隆失败");
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                CloneProgress += $"发生错误: {ex.Message}\n";
                CloneLog += $"发生错误: {ex.Message}\n";
                CloneStatusMessage = $"发生错误: {ex.Message}";
                
                await _errorDisplayService.ShowErrorAsync($"克隆过程中发生错误: {ex.Message}", "错误");
                
                return false;
            }
            finally
            {
                IsCloning = false;
            }
        }

        private async Task ValidateRepositoryUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(RepositoryUrl))
            {
                IsUrlValid = true;
                UrlValidationMessage = string.Empty;
                return;
            }

            // 先进行基本的URL格式验证
            if (!IsValidGitUrlFormat(RepositoryUrl))
            {
                IsUrlValid = false;
                UrlValidationMessage = "无效的Git仓库URL格式";
                return;
            }

            try
            {
                // 设置为验证中状态
                UrlValidationMessage = "正在验证URL...";
                
                var isValid = await _gitService.IsValidGitUrlAsync(RepositoryUrl);
                IsUrlValid = isValid;
                UrlValidationMessage = isValid ? string.Empty : "无效的Git仓库URL或仓库不可访问";
            }
            catch
            {
                IsUrlValid = false;
                UrlValidationMessage = "验证URL时发生错误";
            }
        }

        private bool IsValidGitUrlFormat(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // 检查常见的Git URL格式
            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ExtractProjectNameAsync()
        {
            if (string.IsNullOrWhiteSpace(RepositoryUrl))
            {
                ProjectName = string.Empty;
                return;
            }

            try
            {
                var name = await _gitService.GetRepositoryNameFromUrlAsync(RepositoryUrl);
                if (!string.IsNullOrEmpty(name))
                {
                    ProjectName = name;
                }
            }
            catch
            {
                // 忽略错误，保持当前项目名称
            }
        }

        private async Task AddClonedProjectToManagerAsync(string projectPath)
        {
            try
            {
                var project = new Project
                {
                    Name = ProjectName,
                    LocalPath = projectPath,
                    WorkingDirectory = projectPath,
                    Framework = SelectedFramework,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now
                };

                // 获取Git信息
                project.GitInfo = await _gitService.GetGitInfoAsync(projectPath);

                // 添加项目到管理器
                var saveSuccess = await _projectService.SaveProjectAsync(project);
                
                if (saveSuccess)
                {
                    CloneProgress += "项目已成功添加到管理器\n";
                    CloneLog += "项目已成功添加到管理器\n";
                }
                else
                {
                    CloneProgress += "添加项目到管理器失败：项目名称已存在\n";
                    CloneLog += "添加项目到管理器失败：项目名称已存在\n";
                }
            }
            catch (Exception ex)
            {
                CloneProgress += $"添加项目到管理器失败: {ex.Message}\n";
                CloneLog += $"添加项目到管理器失败: {ex.Message}\n";
            }
        }
    }
}