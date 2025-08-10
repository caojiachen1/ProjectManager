using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class DotNetProjectSettingsViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private Project? _project;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private string _projectPath = string.Empty;

        [ObservableProperty]
        private string _startCommand = "dotnet run";

        [ObservableProperty]
        private string _targetFramework = "net8.0";

        [ObservableProperty]
        private string _projectType = "Web API";

        [ObservableProperty]
        private int _port = 5000;

        [ObservableProperty]
        private bool _enableHotReload = false;

        [ObservableProperty]
        private bool _enableHttpsRedirection = false;

        [ObservableProperty]
        private bool _enableDeveloperExceptionPage = false;

        [ObservableProperty]
        private string _buildConfiguration = "Debug";

        [ObservableProperty]
        private string _buildCommand = "dotnet build";

        [ObservableProperty]
        private string _testCommand = "dotnet test";

        [ObservableProperty]
        private string _outputPath = "./bin/Debug";

        [ObservableProperty]
        private bool _runTestsBeforeBuild = false;

        [ObservableProperty]
        private bool _enableCodeAnalysis = false;

        [ObservableProperty]
        private bool _treatWarningsAsErrors = false;

        [ObservableProperty]
        private string _publishCommand = "dotnet publish";

        [ObservableProperty]
        private string _targetRuntime = "portable";

        [ObservableProperty]
        private string _publishPath = "./publish";

        [ObservableProperty]
        private bool _singleFilePublish = false;

        [ObservableProperty]
        private bool _selfContainedPublish = false;

        [ObservableProperty]
        private bool _enableReadyToRun = false;

        [ObservableProperty]
        private string _tagsString = ".NET,C#,后端,Web API";

        public event EventHandler<Project>? ProjectSaved;
        public event EventHandler<string>? ProjectDeleted;
        public event EventHandler? DialogCancelled;

        public DotNetProjectSettingsViewModel(IProjectService projectService)
        {
            _projectService = projectService;
            
            // 监听属性变化来动态更新启动命令
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EnableHotReload) || 
                e.PropertyName == nameof(BuildConfiguration) ||
                e.PropertyName == nameof(Port))
            {
                UpdateStartCommand();
            }
        }

        private void UpdateStartCommand()
        {
            var command = EnableHotReload ? "dotnet watch run" : "dotnet run";
            
            if (BuildConfiguration == "Release")
                command += " --configuration Release";
                
            if (Port != 5000 && ProjectType.Contains("Web"))
                command += $" --urls http://localhost:{Port}";
            
            StartCommand = command;
        }

        public void LoadProject(Project project)
        {
            _project = project;
            
            ProjectName = project.Name ?? string.Empty;
            ProjectPath = project.LocalPath ?? string.Empty;
            StartCommand = project.StartCommand ?? "dotnet run";
            TagsString = project.Tags != null ? string.Join(", ", project.Tags) : ".NET,C#,后端,Web API";
            
            // 解析.NET特定的启动参数
            ParseStartCommand(project.StartCommand ?? "dotnet run");
        }

        private void ParseStartCommand(string command)
        {
            EnableHotReload = command.Contains("watch");
            BuildConfiguration = command.Contains("--configuration Release") ? "Release" : "Debug";
            
            // 解析端口
            var urlsMatch = System.Text.RegularExpressions.Regex.Match(command, @"--urls\s+http://localhost:(\d+)");
            if (urlsMatch.Success && int.TryParse(urlsMatch.Groups[1].Value, out int port))
            {
                Port = port;
            }
        }

        [RelayCommand]
        private void BrowseOutputPath()
        {
            BrowseFolderPath("选择输出文件夹", path => OutputPath = path);
        }

        [RelayCommand]
        private void BrowsePublishPath()
        {
            BrowseFolderPath("选择发布文件夹", path => PublishPath = path);
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
