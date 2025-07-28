using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class ProjectEditDialogViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private AiProject? _originalProject;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private string _projectDescription = string.Empty;

        [ObservableProperty]
        private string _localPath = string.Empty;

        [ObservableProperty]
        private string _workingDirectory = string.Empty;

        [ObservableProperty]
        private string _startCommand = string.Empty;

        [ObservableProperty]
        private string _pythonEnvironment = string.Empty;

        [ObservableProperty]
        private string _framework = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availableFrameworks = new();

        [ObservableProperty]
        private ObservableCollection<string> _frameworkCommands = new();

        [ObservableProperty]
        private string _author = string.Empty;

        [ObservableProperty]
        private string _version = "1.0.0";

        [ObservableProperty]
        private string _port = "0";

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _tagsString = string.Empty;

        [ObservableProperty]
        private bool _isEditing = false;

        public event EventHandler<AiProject>? ProjectSaved;
        public event EventHandler<string>? ProjectDeleted;
        public event EventHandler? DialogCancelled;

        public ProjectEditDialogViewModel(IProjectService projectService)
        {
            _projectService = projectService;
            
            // 初始化可用框架列表
            AvailableFrameworks = new ObservableCollection<string>(FrameworkConfigService.GetFrameworkNames());
            
            // 监听框架变化
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Framework))
            {
                OnFrameworkChanged();
            }
            else if (e.PropertyName == nameof(LocalPath))
            {
                OnLocalPathChanged();
            }
        }

        private void OnFrameworkChanged()
        {
            if (string.IsNullOrEmpty(Framework))
                return;

            var config = FrameworkConfigService.GetFrameworkConfig(Framework);
            if (config != null)
            {
                // 更新命令建议
                FrameworkCommands = new ObservableCollection<string>(config.CommonCommands);
                
                // 如果启动命令为空，设置默认命令
                if (string.IsNullOrEmpty(StartCommand))
                {
                    StartCommand = config.DefaultStartCommand;
                }
                
                // 如果端口为空或为0，设置默认端口
                if (string.IsNullOrEmpty(Port) || Port == "0")
                {
                    Port = config.DefaultPort.ToString();
                }
                
                // 如果标签为空，设置默认标签
                if (string.IsNullOrEmpty(TagsString))
                {
                    TagsString = string.Join(", ", config.DefaultTags);
                }
            }
        }

        private void OnLocalPathChanged()
        {
            if (!string.IsNullOrEmpty(LocalPath) && string.IsNullOrEmpty(Framework))
            {
                // 自动检测框架类型
                var detectedFramework = FrameworkConfigService.DetectFramework(LocalPath);
                if (detectedFramework != "其他")
                {
                    Framework = detectedFramework;
                }
            }
        }

        public void LoadProject(AiProject? project = null)
        {
            _originalProject = project;
            
            if (project != null)
            {
                IsEditing = true;
                ProjectName = project.Name;
                ProjectDescription = project.Description;
                LocalPath = project.LocalPath;
                WorkingDirectory = project.WorkingDirectory;
                StartCommand = project.StartCommand;
                PythonEnvironment = project.PythonEnvironment;
                Framework = project.Framework;
                Author = project.Author;
                Version = project.Version;
                Port = project.Port.ToString();
                AutoStart = project.AutoStart;
                TagsString = string.Join(", ", project.Tags);
            }
            else
            {
                IsEditing = false;
                // 设置默认值
                Version = "1.0.0";
                Port = "0";
                AutoStart = false;
                FrameworkCommands.Clear();
            }
        }

        [RelayCommand]
        private void BrowseLocalPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择项目文件夹",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(path))
                {
                    LocalPath = path;
                    
                    // 如果工作目录为空，自动设置为项目路径
                    if (string.IsNullOrEmpty(WorkingDirectory))
                    {
                        WorkingDirectory = path;
                    }
                    
                    // 如果项目名称为空，自动设置为文件夹名称
                    if (string.IsNullOrEmpty(ProjectName))
                    {
                        ProjectName = Path.GetFileName(path);
                    }
                    
                    // 触发路径变化事件来自动检测框架
                    OnLocalPathChanged();
                }
            }
        }

        [RelayCommand]
        private void ApplyFrameworkCommand(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                StartCommand = command;
            }
        }

        [RelayCommand]
        private void BrowseWorkingDirectory()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择工作目录",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(path))
                {
                    WorkingDirectory = path;
                }
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            try
            {
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(ProjectName))
                {
                    // TODO: 显示错误消息
                    return;
                }

                if (string.IsNullOrWhiteSpace(LocalPath))
                {
                    // TODO: 显示错误消息
                    return;
                }

                // 创建或更新项目
                var project = _originalProject ?? new AiProject();
                
                project.Name = ProjectName.Trim();
                project.Description = ProjectDescription?.Trim() ?? string.Empty;
                project.LocalPath = LocalPath.Trim();
                project.WorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory) ? LocalPath.Trim() : WorkingDirectory.Trim();
                project.StartCommand = StartCommand?.Trim() ?? string.Empty;
                project.PythonEnvironment = PythonEnvironment?.Trim() ?? string.Empty;
                project.Framework = Framework?.Trim() ?? string.Empty;
                project.Author = Author?.Trim() ?? string.Empty;
                project.Version = Version?.Trim() ?? "1.0.0";
                project.Port = int.TryParse(Port, out var port) ? port : 0;
                project.AutoStart = AutoStart;
                
                // 解析标签
                if (!string.IsNullOrWhiteSpace(TagsString))
                {
                    project.Tags = TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(tag => tag.Trim())
                                           .Where(tag => !string.IsNullOrEmpty(tag))
                                           .ToList();
                }
                else
                {
                    project.Tags = new List<string>();
                }

                project.LastModified = DateTime.Now;
                
                if (_originalProject == null)
                {
                    project.CreatedDate = DateTime.Now;
                }

                await _projectService.SaveProjectAsync(project);
                
                ProjectSaved?.Invoke(this, project);
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
            if (_originalProject != null)
            {
                try
                {
                    // TODO: 显示确认对话框
                    await _projectService.DeleteProjectAsync(_originalProject.Id);
                    ProjectDeleted?.Invoke(this, _originalProject.Id);
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
