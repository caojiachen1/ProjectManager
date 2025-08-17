using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class ProjectEditDialogViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private readonly IErrorDisplayService _errorDisplayService;
        private Project? _originalProject;

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
        private string _framework = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availableFrameworks = new();

        [ObservableProperty]
        private ObservableCollection<string> _frameworkCommands = new();

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _tagsString = string.Empty;

        [ObservableProperty]
        private bool _isEditing = false;

        public event EventHandler<Project>? ProjectSaved;
        public event EventHandler<string>? ProjectDeleted;
        public event EventHandler? DialogCancelled;

        public ProjectEditDialogViewModel(IProjectService projectService, IErrorDisplayService errorDisplayService)
        {
            _projectService = projectService;
            _errorDisplayService = errorDisplayService;
            
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
                
                // 如果标签为空，设置默认标签
                if (string.IsNullOrEmpty(TagsString))
                {
                    TagsString = string.Join(", ", config.DefaultTags);
                }
            }
        }

        public void LoadProject(Project? project = null)
        {
            _originalProject = project;
            
            if (project != null)
            {
                IsEditing = true;
                ProjectName = project.Name ?? string.Empty;
                ProjectDescription = project.Description ?? string.Empty;
                LocalPath = project.LocalPath ?? string.Empty;
                WorkingDirectory = project.WorkingDirectory ?? string.Empty;
                StartCommand = project.StartCommand ?? string.Empty;
                Framework = project.Framework ?? string.Empty;
                AutoStart = project.AutoStart;
                TagsString = project.Tags != null ? string.Join(", ", project.Tags) : string.Empty;
            }
            else
            {
                IsEditing = false;
                // 清空所有字段
                ProjectName = string.Empty;
                ProjectDescription = string.Empty;
                LocalPath = string.Empty;
                WorkingDirectory = string.Empty;
                StartCommand = string.Empty;
                Framework = string.Empty;
                AutoStart = false;
                TagsString = string.Empty;
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
                    await ShowErrorMessage("项目名称不能为空");
                    return;
                }

                if (string.IsNullOrWhiteSpace(LocalPath))
                {
                    await ShowErrorMessage("项目路径不能为空");
                    return;
                }

                // 创建或更新项目
                var project = _originalProject ?? new Project();
                
                project.Name = ProjectName.Trim();
                project.Description = ProjectDescription?.Trim() ?? string.Empty;
                project.LocalPath = LocalPath.Trim();
                project.WorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory) ? LocalPath.Trim() : WorkingDirectory.Trim();
                project.StartCommand = StartCommand?.Trim() ?? string.Empty;
                project.Framework = Framework?.Trim() ?? string.Empty;
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

                var saveSuccess = await _projectService.SaveProjectAsync(project);
                
                if (saveSuccess)
                {
                    ProjectSaved?.Invoke(this, project);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessage($"保存项目失败: {ex.Message}");
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
                    var confirm = await _errorDisplayService.ShowConfirmationAsync($"确定要删除项目 '{_originalProject.Name}' 吗？\n此操作不可撤销。", "确认删除");
                    if (confirm)
                    {
                        await _projectService.DeleteProjectAsync(_originalProject.Id);
                        ProjectDeleted?.Invoke(this, _originalProject.Id);
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorMessage($"删除项目失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"删除项目失败: {ex.Message}");
                }
            }
        }

        private async Task ShowErrorMessage(string message)
        {
            await _errorDisplayService.ShowErrorAsync(message, "错误");
        }
    }
}
