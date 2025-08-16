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
using System.Text.RegularExpressions;

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
                    // 确保在UI线程上更新
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // 逐行追加原始输出，保持日志完整
                        CloneProgress += message + "\n";
                        CloneLog += message + "\n";
                        CloneStatusMessage = message;

                        // 解析 git clone 的各个阶段进度
                        try
                        {
                            var progressUpdated = false;
                            
                            // 匹配 "Receiving objects: XX% (YY/ZZ)" 格式
                            var receivingMatch = Regex.Match(message, @"Receiving objects:\s*(\d{1,3})%");
                            if (receivingMatch.Success && int.TryParse(receivingMatch.Groups[1].Value, out var receivingPct))
                            {
                                // Receiving objects 阶段通常占总进度的 70%
                                CloneProgressValue = Math.Min(70, receivingPct * 70 / 100);
                                progressUpdated = true;
                            }

                            // 匹配 "Resolving deltas: XX% (YY/ZZ)" 格式  
                            var resolvingMatch = Regex.Match(message, @"Resolving deltas:\s*(\d{1,3})%");
                            if (resolvingMatch.Success && int.TryParse(resolvingMatch.Groups[1].Value, out var resolvingPct))
                            {
                                // Resolving deltas 阶段从 70% 开始到 95%
                                CloneProgressValue = 70 + Math.Min(25, resolvingPct * 25 / 100);
                                progressUpdated = true;
                            }

                            // 匹配其他包含百分比的输出（如 remote: Counting objects: 100%）
                            if (!progressUpdated)
                            {
                                var generalMatch = Regex.Match(message, @"(\d{1,3})%");
                                if (generalMatch.Success && int.TryParse(generalMatch.Groups[1].Value, out var generalPct))
                                {
                                    // 对于其他阶段，使用相对保守的进度更新
                                    if (message.Contains("Counting") || message.Contains("Compressing"))
                                    {
                                        CloneProgressValue = Math.Max(CloneProgressValue, Math.Min(30, generalPct * 30 / 100));
                                        progressUpdated = true;
                                    }
                                }
                            }

                            // 基于关键字的阶段性进度更新（作为兜底）
                            if (!progressUpdated)
                            {
                                if (message.Contains("开始克隆") || message.Contains("Cloning into"))
                                    CloneProgressValue = Math.Max(CloneProgressValue, 5);
                                else if (message.Contains("remote:") && message.Contains("Enumerating"))
                                    CloneProgressValue = Math.Max(CloneProgressValue, 10);
                                else if (message.Contains("remote:") && message.Contains("Total"))
                                    CloneProgressValue = Math.Max(CloneProgressValue, 35);
                                else if (message.Contains("done") && !message.Contains("Resolving"))
                                    CloneProgressValue = Math.Max(CloneProgressValue, 95);
                                else if (message.Contains("克隆完成") || message.Contains("克隆成功"))
                                    CloneProgressValue = 100;
                            }

                            // 确保进度值在合理范围内
                            CloneProgressValue = Math.Max(0, Math.Min(100, CloneProgressValue));
                        }
                        catch
                        {
                            // 忽略解析错误，保持已有进度值
                        }
                    }));
                });

                var result = await _gitService.CloneRepositoryAsync(RepositoryUrl, finalLocalPath, progress);

                if (result.IsSuccess)
                {
                    CloneProgress += "克隆成功！\n";
                    CloneLog += "克隆成功！\n";
                    CloneStatusMessage = "克隆成功！";

                    // 确保进度为 100%
                    CloneProgressValue = 100;

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

                    // 使用错误显示服务显示信息窗口（表示操作完成）
                    try
                    {
                        await _errorDisplayService.ShowInfoAsync($"仓库已成功克隆到：{finalLocalPath}", "克隆完成");
                    }
                    catch
                    {
                        // 忽略展示错误，不影响流程
                    }

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