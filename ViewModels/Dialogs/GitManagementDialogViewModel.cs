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

        public event EventHandler<Project>? GitInfoUpdated;

        public GitManagementDialogViewModel(IGitService gitService, IContentDialogService contentDialogService)
        {
            _gitService = gitService;
            _contentDialogService = contentDialogService;
        }

        public async Task LoadProjectAsync(Project project)
        {
            Project = project;
            await RefreshGitInfoAsync();
        }

        [RelayCommand]
        private async Task RefreshGitInfo()
        {
            await RefreshGitInfoAsync();
        }

        [RelayCommand]
        private async Task InitializeRepository()
        {
            if (Project == null) return;

            IsLoading = true;
            try
            {
                var success = await _gitService.InitializeRepositoryAsync(Project.LocalPath);
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
                var success = await _gitService.AddAllAsync(Project.LocalPath);
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
                var success = await _gitService.CommitAsync(Project.LocalPath, CommitMessage);
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
                var success = await _gitService.PushAsync(Project.LocalPath);
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
                var success = await _gitService.PullAsync(Project.LocalPath);
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
                var success = await _gitService.CreateBranchAsync(Project.LocalPath, NewBranchName);
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
                var success = await _gitService.SwitchBranchAsync(Project.LocalPath, SelectedBranch);
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
                var success = await _gitService.SetRemoteUrlAsync(Project.LocalPath, RemoteUrl);
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
                GitInfo = await _gitService.GetGitInfoAsync(Project.LocalPath);
                Project.GitInfo = GitInfo;

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
            var dialog = new ContentDialog
            {
                Title = "成功",
                Content = message,
                PrimaryButtonText = "确定"
            };
            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }

        private async Task ShowErrorMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = message,
                PrimaryButtonText = "确定"
            };
            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }
    }
}
