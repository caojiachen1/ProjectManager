using Wpf.Ui.Appearance;

namespace ProjectManager.Models
{
    public class AppSettings
    {
        // Git设置
        public string GitExecutablePath { get; set; } = string.Empty;
        public string GitUserName { get; set; } = string.Empty;
        public string GitUserEmail { get; set; } = string.Empty;
        public string DefaultGitBranch { get; set; } = "main";
        public bool AutoFetchGitUpdates { get; set; } = true;

        // 项目设置
        public string DefaultProjectPath { get; set; } = string.Empty;
        public bool AutoStartProjects { get; set; } = false;
        public bool AutoSaveProjects { get; set; } = true;
        public int ProjectRefreshInterval { get; set; } = 30;
        public int MaxRecentProjects { get; set; } = 10;

        // 应用程序设置
        public string PreferredTerminal { get; set; } = "PowerShell";
        public bool ShowNotifications { get; set; } = true;
        public bool UseCmdChcp65001 { get; set; } = true;

        // 个性化设置
        public ApplicationTheme Theme { get; set; } = ApplicationTheme.Unknown;
    }
}