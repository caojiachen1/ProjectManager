using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectManager.Models
{
    /// <summary>
    /// Git信息模型
    /// </summary>
    public partial class GitInfo : ObservableObject
    {
        [ObservableProperty]
        private bool _isGitRepository = false;

        [ObservableProperty]
        private string _currentBranch = string.Empty;

        [ObservableProperty]
        private string _remoteUrl = string.Empty;

        [ObservableProperty]
        private int _uncommittedChanges = 0;

        [ObservableProperty]
        private int _unpushedCommits = 0;

        [ObservableProperty]
        private string _lastCommitMessage = string.Empty;

        [ObservableProperty]
        private DateTime _lastCommitDate = DateTime.MinValue;

        [ObservableProperty]
        private string _lastCommitAuthor = string.Empty;

        [ObservableProperty]
        private List<string> _branches = new();

        [ObservableProperty]
        private GitStatus _status = GitStatus.Clean;

        public string StatusDisplay => Status switch
        {
            GitStatus.Clean => Application.Current.FindResource("GitStatus_Clean")?.ToString() ?? "Clean",
            GitStatus.Modified => Application.Current.FindResource("GitStatus_Modified")?.ToString() ?? "Modified",
            GitStatus.Staged => Application.Current.FindResource("GitStatus_Staged")?.ToString() ?? "Staged",
            GitStatus.Conflicted => Application.Current.FindResource("GitStatus_Conflicted")?.ToString() ?? "Conflicted",
            GitStatus.Untracked => Application.Current.FindResource("GitStatus_Untracked")?.ToString() ?? "Untracked",
            _ => Application.Current.FindResource("Status_Unknown")?.ToString() ?? "Unknown"
        };

        public string LastCommitDateDisplay => LastCommitDate == DateTime.MinValue 
            ? "无提交" 
            : LastCommitDate.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public enum GitStatus
    {
        Clean,      // 干净状态
        Modified,   // 有修改
        Staged,     // 已暂存
        Conflicted, // 有冲突
        Untracked   // 有未跟踪文件
    }
}
