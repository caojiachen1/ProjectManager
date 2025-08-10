using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class ComfyUIProjectSettingsViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private Project? _project;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private string _projectPath = string.Empty;

        [ObservableProperty]
        private string _startCommand = "python main.py";

        [ObservableProperty]
        private int _port = 8188;

        [ObservableProperty]
        private string _pythonPath = string.Empty;

        [ObservableProperty]
        private bool _listenAllInterfaces = false;

        [ObservableProperty]
        private bool _lowVramMode = false;

        [ObservableProperty]
        private bool _cpuMode = false;

        [ObservableProperty]
        private string _modelsPath = "./models";

        [ObservableProperty]
        private string _outputPath = "./output";

        [ObservableProperty]
        private string _extraArgs = string.Empty;

        [ObservableProperty]
        private string _customNodesPath = "./custom_nodes";

        [ObservableProperty]
        private bool _autoLoadWorkflow = true;

        [ObservableProperty]
        private bool _enableWorkflowSnapshots = false;

        [ObservableProperty]
        private string _tagsString = "AI绘画,图像生成,工作流,节点编辑";

        public event EventHandler<Project>? ProjectSaved;
        public event EventHandler<string>? ProjectDeleted;
        public event EventHandler? DialogCancelled;

        public ComfyUIProjectSettingsViewModel(IProjectService projectService)
        {
            _projectService = projectService;
            
            // 监听属性变化来动态更新启动命令
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ListenAllInterfaces) || 
                e.PropertyName == nameof(Port) || 
                e.PropertyName == nameof(CpuMode) ||
                e.PropertyName == nameof(LowVramMode))
            {
                UpdateStartCommand();
            }
        }

        private void UpdateStartCommand()
        {
            var command = "python main.py";
            
            if (ListenAllInterfaces)
                command += " --listen";
            
            if (Port != 8188)
                command += $" --port {Port}";
            
            if (CpuMode)
                command += " --cpu";
            else if (LowVramMode)
                command += " --lowvram";
            
            if (!string.IsNullOrEmpty(ExtraArgs))
                command += $" {ExtraArgs}";
            
            StartCommand = command;
        }

        public void LoadProject(Project project)
        {
            _project = project;
            
            ProjectName = project.Name ?? string.Empty;
            ProjectPath = project.LocalPath ?? string.Empty;
            StartCommand = project.StartCommand ?? "python main.py";
            TagsString = project.Tags != null ? string.Join(", ", project.Tags) : "AI绘画,图像生成,工作流,节点编辑";
            
            // 解析ComfyUI特定的启动参数
            ParseStartCommand(project.StartCommand ?? "python main.py");
        }

        private void ParseStartCommand(string command)
        {
            ListenAllInterfaces = command.Contains("--listen");
            CpuMode = command.Contains("--cpu");
            LowVramMode = command.Contains("--lowvram");
            
            // 解析端口
            var portMatch = System.Text.RegularExpressions.Regex.Match(command, @"--port\s+(\d+)");
            if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out int port))
            {
                Port = port;
            }
        }

        [RelayCommand]
        private void BrowsePythonPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择Python可执行文件",
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                InitialDirectory = string.IsNullOrEmpty(PythonPath) ? 
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : 
                    Path.GetDirectoryName(PythonPath)
            };

            if (dialog.ShowDialog() == true)
            {
                PythonPath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseModelsPath()
        {
            BrowseFolderPath("选择模型文件夹", path => ModelsPath = path);
        }

        [RelayCommand]
        private void BrowseOutputPath()
        {
            BrowseFolderPath("选择输出文件夹", path => OutputPath = path);
        }

        [RelayCommand]
        private void BrowseCustomNodesPath()
        {
            BrowseFolderPath("选择自定义节点文件夹", path => CustomNodesPath = path);
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
