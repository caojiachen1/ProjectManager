using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectManager.Models
{
    public partial class Project : ObservableObject
    {
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _localPath = string.Empty;

        [ObservableProperty]
        private string _startCommand = string.Empty;

        [ObservableProperty]
        private string _workingDirectory = string.Empty;

        [ObservableProperty]
        private string _framework = string.Empty;

        [ObservableProperty]
        [JsonIgnore]
        private GitInfo? _gitInfo;

        [ObservableProperty]
        private DateTime _createdDate = DateTime.Now;

        [ObservableProperty]
        private DateTime _lastModified = DateTime.Now;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusDisplay))]
        private ProjectStatus _status = ProjectStatus.Stopped;

        [ObservableProperty]
        [JsonIgnore]
        private Process? _runningProcess;

        [ObservableProperty]
        private string _logOutput = string.Empty;

        [ObservableProperty]
        private List<string> _tags = new();

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private Dictionary<string, string> _environmentVariables = new();

        [ObservableProperty]
        private List<string> _gitRepositories = new();

        [JsonIgnore]
        public string StatusDisplay => Status switch
        {
            ProjectStatus.Running => "运行中",
            ProjectStatus.Stopped => "已停止",
            ProjectStatus.Starting => "启动中",
            ProjectStatus.Stopping => "停止中",
            ProjectStatus.Error => "错误",
            _ => "未知"
        };

        [JsonIgnore]
        public string LastModifiedDisplay => LastModified.ToString("yyyy-MM-dd HH:mm:ss");

        [JsonIgnore]
        public string CreatedDateDisplay => CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public enum ProjectStatus
    {
        [Description("已停止")]
        Stopped,
        [Description("运行中")]
        Running,
        [Description("启动中")]
        Starting,
        [Description("停止中")]
        Stopping,
        [Description("错误")]
        Error
    }
}
