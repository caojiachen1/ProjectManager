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

        // ComboBox选项集合
        public List<string> StartCommandOptions { get; } = new()
        {
            "dotnet run",
            "dotnet watch run", 
            "dotnet run --launch-profile Development",
            "dotnet run --no-build",
            "dotnet run --configuration Release",
            "dotnet run --project ."
        };

        public List<string> TargetFrameworkOptions { get; } = new()
        {
            "net8.0", "net7.0", "net6.0", "netcoreapp3.1", "net48", "netstandard2.1"
        };

        public List<string> ProjectTypeOptions { get; } = new()
        {
            "Web API", "Web App (MVC)", "Blazor Server", "Blazor WebAssembly",
            "Console App", "Class Library", "Worker Service", "WPF", "WinForms"
        };

        public List<string> BuildConfigurationOptions { get; } = new()
        {
            "Debug", "Release"
        };

        public List<string> TargetRuntimeOptions { get; } = new()
        {
            "portable", "win-x64", "win-x86", "linux-x64", "osx-x64", "osx-arm64"
        };

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
            else if (e.PropertyName == nameof(StartCommand))
            {
                // 当启动命令变化时，解析命令并更新相关设置
                ParseStartCommandAndUpdateSettings(StartCommand);
            }
        }

        private void UpdateStartCommand()
        {
            if (_isUpdatingFromCommand) return; // 防止循环更新
            
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

            // 如果已有持久化设置，覆盖解析值
            if (project.DotNetSettings != null)
            {
                TargetFramework = project.DotNetSettings.TargetFramework;
                ProjectType = project.DotNetSettings.ProjectType;
                Port = project.DotNetSettings.Port;
                EnableHotReload = project.DotNetSettings.EnableHotReload;
                EnableHttpsRedirection = project.DotNetSettings.EnableHttpsRedirection;
                EnableDeveloperExceptionPage = project.DotNetSettings.EnableDeveloperExceptionPage;
                BuildConfiguration = project.DotNetSettings.BuildConfiguration;
                BuildCommand = project.DotNetSettings.BuildCommand;
                TestCommand = project.DotNetSettings.TestCommand;
                OutputPath = project.DotNetSettings.OutputPath;
                RunTestsBeforeBuild = project.DotNetSettings.RunTestsBeforeBuild;
                EnableCodeAnalysis = project.DotNetSettings.EnableCodeAnalysis;
                TreatWarningsAsErrors = project.DotNetSettings.TreatWarningsAsErrors;
                PublishCommand = project.DotNetSettings.PublishCommand;
                TargetRuntime = project.DotNetSettings.TargetRuntime;
                PublishPath = project.DotNetSettings.PublishPath;
                SingleFilePublish = project.DotNetSettings.SingleFilePublish;
                SelfContainedPublish = project.DotNetSettings.SelfContainedPublish;
                EnableReadyToRun = project.DotNetSettings.EnableReadyToRun;
            }
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

        private bool _isUpdatingFromCommand = false;

        private void ParseStartCommandAndUpdateSettings(string command)
        {
            if (_isUpdatingFromCommand || string.IsNullOrWhiteSpace(command)) return; // 防止循环更新
            
            _isUpdatingFromCommand = true;
            try
            {
                // 解析热重载设置
                var newEnableHotReload = command.Contains("watch");
                if (EnableHotReload != newEnableHotReload)
                    EnableHotReload = newEnableHotReload;

                // 解析构建配置
                var newBuildConfiguration = command.Contains("--configuration Release") ? "Release" : "Debug";
                if (BuildConfiguration != newBuildConfiguration)
                    BuildConfiguration = newBuildConfiguration;

                // 解析端口
                var urlsMatch = System.Text.RegularExpressions.Regex.Match(command, @"--urls\s+https?://[^:]*:(\d+)");
                if (urlsMatch.Success && int.TryParse(urlsMatch.Groups[1].Value, out int port))
                {
                    if (Port != port)
                        Port = port;
                }

                // 根据命令内容推断项目类型和设置
                if (command.Contains("--urls"))
                {
                    // 包含URL参数，很可能是Web项目
                    if (!ProjectType.Contains("Web") && !ProjectType.Contains("Blazor"))
                    {
                        ProjectType = "Web API";
                    }
                    
                    // 如果有HTTPS URL，启用HTTPS重定向
                    if (command.Contains("https://"))
                    {
                        EnableHttpsRedirection = true;
                    }
                }

                // 解析启动配置文件
                if (command.Contains("--launch-profile"))
                {
                    var profileMatch = System.Text.RegularExpressions.Regex.Match(command, @"--launch-profile\s+(\w+)");
                    if (profileMatch.Success)
                    {
                        var profile = profileMatch.Groups[1].Value;
                        if (profile.Equals("Development", StringComparison.OrdinalIgnoreCase))
                        {
                            EnableDeveloperExceptionPage = true;
                        }
                    }
                }

                // 如果是开发模式的watch命令，启用一些开发者友好的设置
                if (command.Contains("watch") && BuildConfiguration == "Debug")
                {
                    EnableDeveloperExceptionPage = true;
                }
            }
            finally
            {
                _isUpdatingFromCommand = false;
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
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = title,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                setPath(dialog.FolderName);
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

                // 写入 .NET 设置
                if (_project.Framework.Equals(".NET", StringComparison.OrdinalIgnoreCase))
                {
                    _project.DotNetSettings = new DotNetSettings
                    {
                        TargetFramework = TargetFramework,
                        ProjectType = ProjectType,
                        Port = Port,
                        EnableHotReload = EnableHotReload,
                        EnableHttpsRedirection = EnableHttpsRedirection,
                        EnableDeveloperExceptionPage = EnableDeveloperExceptionPage,
                        BuildConfiguration = BuildConfiguration,
                        BuildCommand = BuildCommand,
                        TestCommand = TestCommand,
                        OutputPath = OutputPath,
                        RunTestsBeforeBuild = RunTestsBeforeBuild,
                        EnableCodeAnalysis = EnableCodeAnalysis,
                        TreatWarningsAsErrors = TreatWarningsAsErrors,
                        PublishCommand = PublishCommand,
                        TargetRuntime = TargetRuntime,
                        PublishPath = PublishPath,
                        SingleFilePublish = SingleFilePublish,
                        SelfContainedPublish = SelfContainedPublish,
                        EnableReadyToRun = EnableReadyToRun
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
                else
                {
                    // TODO: 使用WPF UI消息框显示错误
                    System.Windows.MessageBox.Show("保存项目失败，请检查项目路径和权限。", "错误", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // TODO: 使用WPF UI消息框显示错误  
                System.Windows.MessageBox.Show($"保存项目时发生错误：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    // 显示确认对话框
                    var result = System.Windows.MessageBox.Show(
                        $"确定要删除项目 '{_project.Name}' 吗？\n\n注意：这只会从项目列表中移除，不会删除实际文件。", 
                        "确认删除", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Question);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        await _projectService.DeleteProjectAsync(_project.Id);
                        ProjectDeleted?.Invoke(this, _project.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"删除项目时发生错误：{ex.Message}", "错误",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}
