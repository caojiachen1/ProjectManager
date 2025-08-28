using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Models;
using ProjectManager.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class GitManagementDialogViewModel : ObservableObject
    {
        private readonly IGitService _gitService;
        private readonly IContentDialogService _contentDialogService;
        private readonly IErrorDisplayService _errorDisplayService;
        private bool _isPopulatingRepositories = false;

        [ObservableProperty]
        private Project? _project;

        [ObservableProperty]
        private GitInfo? _gitInfo;

        [ObservableProperty]
        private string _commitMessage = string.Empty;

        [ObservableProperty]
        private string _newBranchName = string.Empty;

        [ObservableProperty]
        private string _selectedBranch = string.Empty;

        [ObservableProperty]
        private string _remoteUrl = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private ObservableCollection<string> _availableBranches = new();

        [ObservableProperty]
        private ObservableCollection<GitRepositoryInfo> _availableRepositories = new();

        [ObservableProperty]
        private GitRepositoryInfo? _selectedRepository;

        [ObservableProperty]
        private bool _hasMultipleRepositories = false;

        [ObservableProperty]
        private string _currentRepositoryPath = string.Empty;

        public event EventHandler<Project>? GitInfoUpdated;

        public GitManagementDialogViewModel(IGitService gitService, IContentDialogService contentDialogService, IErrorDisplayService errorDisplayService)
        {
            _gitService = gitService;
            _contentDialogService = contentDialogService;
            _errorDisplayService = errorDisplayService;
        }

        public async Task LoadProjectAsync(Project project)
        {
            Project = project;
            await LoadAvailableRepositoriesAsync();
            await RefreshGitInfoAsync();
        }

        private async Task LoadAvailableRepositoriesAsync()
        {
            if (Project == null) return;

            try
            {
                _isPopulatingRepositories = true;
                AvailableRepositories.Clear();

                // 首先检查项目根目录是否是Git仓库
                var projectRootGitInfo = await _gitService.GetGitInfoAsync(Project.LocalPath);
                if (projectRootGitInfo.IsGitRepository)
                {
                    AvailableRepositories.Add(new GitRepositoryInfo(Project.LocalPath, Project.LocalPath));
                }

                // 添加项目中扫描到的其他Git仓库，但只添加有效的仓库
                if (Project.GitRepositories?.Count > 0)
                {
                    foreach (var repoPath in Project.GitRepositories)
                    {
                        // 避免重复添加项目根目录
                        if (repoPath != Project.LocalPath)
                        {
                            // 再次验证仓库的有效性
                            if (await _gitService.IsValidGitRepositoryAsync(repoPath))
                            {
                                AvailableRepositories.Add(new GitRepositoryInfo(repoPath, Project.LocalPath));
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"跳过无效的Git仓库: {repoPath}");
                            }
                        }
                    }
                }

                HasMultipleRepositories = AvailableRepositories.Count > 1;

                // 设置默认选择的仓库（优先选择主仓库）
                if (AvailableRepositories.Count > 0)
                {
                    var mainRepo = AvailableRepositories.FirstOrDefault(r => r.IsMainRepository);
                    SelectedRepository = mainRepo ?? AvailableRepositories.First();
                    CurrentRepositoryPath = SelectedRepository.Path;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载Git仓库列表失败: {ex.Message}");
            }
            finally
            {
                _isPopulatingRepositories = false;
            }
        }

        partial void OnSelectedRepositoryChanged(GitRepositoryInfo? value)
        {
            if (_isPopulatingRepositories) return;
            if (value == null) return;
            if (value.Path == CurrentRepositoryPath) return;
            CurrentRepositoryPath = value.Path;
            // fire and forget to avoid blocking setter; errors handled inside RefreshGitInfoAsync
            _ = RefreshGitInfoAsync();
        }

        [RelayCommand]
        private async Task RefreshGitInfo()
        {
            await RefreshGitInfoAsync();
        }

        private string GetCurrentRepositoryPath()
        {
            return !string.IsNullOrEmpty(CurrentRepositoryPath) ? CurrentRepositoryPath : Project?.LocalPath ?? string.Empty;
        }

        [RelayCommand]
        private async Task InitializeRepository()
        {
            if (Project == null) return;

            IsLoading = true;
            try
            {
                var success = await _gitService.InitializeRepositoryAsync(GetCurrentRepositoryPath());
                if (success)
                {
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage("Git仓库初始化成功");
                }
                else
                {
                    await ShowErrorMessage("Git仓库初始化失败");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddAllFiles()
        {
            if (Project == null || GitInfo?.IsGitRepository != true) return;

            IsLoading = true;
            try
            {
                var success = await _gitService.AddAllAsync(GetCurrentRepositoryPath());
                if (success)
                {
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage("文件已添加到暂存区");
                }
                else
                {
                    await ShowErrorMessage("添加文件失败");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Commit()
        {
            if (Project == null || GitInfo?.IsGitRepository != true || string.IsNullOrWhiteSpace(CommitMessage))
                return;

            IsLoading = true;
            try
            {
                var success = await _gitService.CommitAsync(GetCurrentRepositoryPath(), CommitMessage);
                if (success)
                {
                    CommitMessage = string.Empty;
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage("提交成功");
                }
                else
                {
                    await ShowErrorMessage("提交失败");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Push()
        {
            if (Project == null || GitInfo?.IsGitRepository != true) return;

            IsLoading = true;
            try
            {
                var success = await _gitService.PushAsync(GetCurrentRepositoryPath());
                if (success)
                {
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage("推送成功");
                }
                else
                {
                    await ShowErrorMessage("推送失败，请检查远程仓库配置");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Pull()
        {
            if (Project == null || GitInfo?.IsGitRepository != true) return;

            IsLoading = true;
            try
            {
                var success = await _gitService.PullAsync(GetCurrentRepositoryPath());
                if (success)
                {
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage("拉取成功");
                }
                else
                {
                    await ShowErrorMessage("拉取失败");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CreateBranch()
        {
            if (Project == null || GitInfo?.IsGitRepository != true || string.IsNullOrWhiteSpace(NewBranchName))
                return;

            IsLoading = true;
            try
            {
                var success = await _gitService.CreateBranchAsync(GetCurrentRepositoryPath(), NewBranchName);
                if (success)
                {
                    NewBranchName = string.Empty;
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage("分支创建成功");
                }
                else
                {
                    await ShowErrorMessage("分支创建失败");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SwitchBranch()
        {
            if (Project == null || GitInfo?.IsGitRepository != true || string.IsNullOrWhiteSpace(SelectedBranch))
                return;

            IsLoading = true;
            try
            {
                var success = await _gitService.SwitchBranchAsync(GetCurrentRepositoryPath(), SelectedBranch);
                if (success)
                {
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage($"已切换到分支: {SelectedBranch}");
                }
                else
                {
                    await ShowErrorMessage("分支切换失败");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SetRemoteUrl()
        {
            if (Project == null || GitInfo?.IsGitRepository != true || string.IsNullOrWhiteSpace(RemoteUrl))
                return;

            IsLoading = true;
            try
            {
                var success = await _gitService.SetRemoteUrlAsync(GetCurrentRepositoryPath(), RemoteUrl);
                if (success)
                {
                    await RefreshGitInfoAsync();
                    await ShowSuccessMessage("远程仓库地址设置成功");
                }
                else
                {
                    await ShowErrorMessage("远程仓库地址设置失败");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshGitInfoAsync()
        {
            if (Project == null) return;

            try
            {
                IsLoading = true;
                
                // 使用当前选择的仓库路径，如果没有选择则使用项目路径
                var repositoryPath = GetCurrentRepositoryPath();
                
                GitInfo = await _gitService.GetGitInfoAsync(repositoryPath);
                
                // 更新项目的Git信息（仅当是主仓库时）
                if (repositoryPath == Project.LocalPath)
                {
                    Project.GitInfo = GitInfo;
                }

                if (GitInfo.IsGitRepository)
                {
                    AvailableBranches = new ObservableCollection<string>(GitInfo.Branches);
                    SelectedBranch = GitInfo.CurrentBranch;
                    RemoteUrl = GitInfo.RemoteUrl;
                }

                GitInfoUpdated?.Invoke(this, Project);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ShowSuccessMessage(string message)
        {
            await _errorDisplayService.ShowInfoAsync(message, "成功");
        }

        private async Task ShowErrorMessage(string message)
        {
            await _errorDisplayService.ShowErrorAsync(message, "错误");
        }
    }
}
