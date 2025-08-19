using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class NodeJSProjectSettingsViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private Project? _project;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private string _projectPath = string.Empty;

        [ObservableProperty]
        private string _startCommand = "npm start";

        [ObservableProperty]
        private int _port = 3000;

        [ObservableProperty]
        private string _nodeVersion = string.Empty;

        [ObservableProperty]
        private string _packageManager = "npm";

        [ObservableProperty]
        private bool _developmentMode = false;

        [ObservableProperty]
        private bool _hotReload = false;

        [ObservableProperty]
        private bool _debugMode = false;

        [ObservableProperty]
        private string _buildCommand = "npm run build";

        [ObservableProperty]
        private string _testCommand = "npm test";

        [ObservableProperty]
        private string _buildOutputPath = "./dist";

        [ObservableProperty]
        private bool _runTestsBeforeBuild = false;

        [ObservableProperty]
        private bool _minifyOutput = false;

        [ObservableProperty]
        private string _environmentFile = ".env";

        [ObservableProperty]
        private string _customEnvironmentVars = string.Empty;

        [ObservableProperty]
        private string _tagsString = "JavaScript,Node.js,后端,全栈";

        public event EventHandler<Project>? ProjectSaved;
        public event EventHandler<string>? ProjectDeleted;
        public event EventHandler? DialogCancelled;

        public NodeJSProjectSettingsViewModel(IProjectService projectService)
        {
            _projectService = projectService;
            
            // 监听属性变化来动态更新启动命令
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DevelopmentMode) || 
                e.PropertyName == nameof(HotReload) || 
                e.PropertyName == nameof(DebugMode) ||
                e.PropertyName == nameof(Port) ||
                e.PropertyName == nameof(PackageManager))
            {
                UpdateStartCommand();
            }
        }

        private void UpdateStartCommand()
        {
            var baseCommand = PackageManager switch
            {
                "yarn" => "yarn",
                "pnpm" => "pnpm",
                _ => "npm run"
            };

            var command = HotReload ? $"{baseCommand} dev" : $"{baseCommand} start";
            
            if (DebugMode)
                command = command.Replace("start", "start --inspect");
            
            StartCommand = command;
        }

        public void LoadProject(Project project)
        {
            _project = project;
            
            ProjectName = project.Name ?? string.Empty;
            ProjectPath = project.LocalPath ?? string.Empty;
            StartCommand = project.StartCommand ?? "npm start";
            TagsString = project.Tags != null ? string.Join(", ", project.Tags) : "JavaScript,Node.js,后端,全栈";
            
            // 解析Node.js特定的启动参数
            ParseStartCommand(project.StartCommand ?? "npm start");

            // 如果已有持久化设置，覆盖解析值
            if (project.NodeJSSettings != null)
            {
                Port = project.NodeJSSettings.Port;
                NodeVersion = project.NodeJSSettings.NodeVersion;
                PackageManager = project.NodeJSSettings.PackageManager;
                DevelopmentMode = project.NodeJSSettings.DevelopmentMode;
                HotReload = project.NodeJSSettings.HotReload;
                DebugMode = project.NodeJSSettings.DebugMode;
                BuildCommand = project.NodeJSSettings.BuildCommand;
                TestCommand = project.NodeJSSettings.TestCommand;
                BuildOutputPath = project.NodeJSSettings.BuildOutputPath;
                RunTestsBeforeBuild = project.NodeJSSettings.RunTestsBeforeBuild;
                MinifyOutput = project.NodeJSSettings.MinifyOutput;
                EnvironmentFile = project.NodeJSSettings.EnvironmentFile;
                CustomEnvironmentVars = project.NodeJSSettings.CustomEnvironmentVars;
            }
        }

        private void ParseStartCommand(string command)
        {
            HotReload = command.Contains("dev");
            DebugMode = command.Contains("--inspect");
            DevelopmentMode = command.Contains("NODE_ENV=development");
            
            if (command.StartsWith("yarn"))
                PackageManager = "yarn";
            else if (command.StartsWith("pnpm"))
                PackageManager = "pnpm";
            else
                PackageManager = "npm";
        }

        [RelayCommand]
        private void BrowseBuildOutputPath()
        {
            BrowseFolderPath("选择构建输出文件夹", path => BuildOutputPath = path);
        }

        [RelayCommand]
        private void BrowseEnvironmentFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择环境变量文件",
                Filter = "环境变量文件 (*.env)|*.env|所有文件 (*.*)|*.*",
                InitialDirectory = string.IsNullOrEmpty(ProjectPath) ? 
                    Environment.CurrentDirectory : ProjectPath
            };

            if (dialog.ShowDialog() == true)
            {
                EnvironmentFile = dialog.FileName;
            }
        }

        private void BrowseFolderPath(string title, Action<string> setPath)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(path))
                {
                    setPath(path);
                }
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (_project == null) return;

            try
            {
                // 更新项目信息
                _project.StartCommand = StartCommand;
                _project.LastModified = DateTime.Now;

                // 写入 Node.js 设置
                if (_project.Framework.Equals("Node.js", StringComparison.OrdinalIgnoreCase))
                {
                    _project.NodeJSSettings = new NodeJSSettings
                    {
                        Port = Port,
                        NodeVersion = NodeVersion,
                        PackageManager = PackageManager,
                        DevelopmentMode = DevelopmentMode,
                        HotReload = HotReload,
                        DebugMode = DebugMode,
                        BuildCommand = BuildCommand,
                        TestCommand = TestCommand,
                        BuildOutputPath = BuildOutputPath,
                        RunTestsBeforeBuild = RunTestsBeforeBuild,
                        MinifyOutput = MinifyOutput,
                        EnvironmentFile = EnvironmentFile,
                        CustomEnvironmentVars = CustomEnvironmentVars
                    };
                }
                
                // 解析标签
                if (!string.IsNullOrWhiteSpace(TagsString))
                {
                    _project.Tags = TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(tag => tag.Trim())
                                              .Where(tag => !string.IsNullOrEmpty(tag))
                                              .ToList();
                }
                else
                {
                    _project.Tags = new List<string>();
                }

                var saveSuccess = await _projectService.SaveProjectAsync(_project);
                
                if (saveSuccess)
                {
                    ProjectSaved?.Invoke(this, _project);
                }
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息
                System.Diagnostics.Debug.WriteLine($"保存项目失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogCancelled?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (_project != null)
            {
                try
                {
                    // TODO: 显示确认对话框
                    await _projectService.DeleteProjectAsync(_project.Id);
                    ProjectDeleted?.Invoke(this, _project.Id);
                }
                catch (Exception ex)
                {
                    // TODO: 显示错误消息
                    System.Diagnostics.Debug.WriteLine($"删除项目失败: {ex.Message}");
                }
            }
        }
    }
}
