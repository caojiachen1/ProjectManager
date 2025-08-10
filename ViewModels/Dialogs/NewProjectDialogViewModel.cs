using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class NewProjectDialogViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private readonly IProjectSettingsWindowService _settingsWindowService;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private string _projectPath = string.Empty;

        [ObservableProperty]
        private string _projectDescription = string.Empty;

        [ObservableProperty]
        private string _selectedFramework = string.Empty;

        [ObservableProperty]
        private string _frameworkDescription = string.Empty;

        [ObservableProperty]
        private bool _canProceed = false;

        public ObservableCollection<string> AvailableFrameworks { get; } = new()
        {
            "ComfyUI",
            "Node.js",
            ".NET",
            "其他"
        };

        public event EventHandler? ProjectCreated;
        public event EventHandler? DialogCancelled;
        public Project? CreatedProject { get; private set; }

        public NewProjectDialogViewModel(IProjectService projectService, IProjectSettingsWindowService settingsWindowService)
        {
            _projectService = projectService;
            _settingsWindowService = settingsWindowService;
            
            // 监听属性变化
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectName) || 
                e.PropertyName == nameof(ProjectPath) || 
                e.PropertyName == nameof(SelectedFramework))
            {
                UpdateCanProceed();
            }
        }

        private void UpdateCanProceed()
        {
            CanProceed = !string.IsNullOrWhiteSpace(ProjectName) && 
                         !string.IsNullOrWhiteSpace(ProjectPath) && 
                         !string.IsNullOrWhiteSpace(SelectedFramework);
        }

        public void SelectFramework(string framework)
        {
            SelectedFramework = framework;
            
            // 获取框架描述
            var config = FrameworkConfigService.GetFrameworkConfig(framework);
            if (config != null)
            {
                FrameworkDescription = $"已选择: {framework} - {config.Description}";
            }
            else
            {
                FrameworkDescription = $"已选择: {framework}";
            }
        }

        [RelayCommand]
        private void BrowseProjectPath()
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
                    ProjectPath = path;
                    
                    // 如果项目名称为空，自动设置为文件夹名称
                    if (string.IsNullOrEmpty(ProjectName))
                    {
                        ProjectName = Path.GetFileName(path);
                    }
                }
            }
        }

        [RelayCommand]
        private async Task Next()
        {
            try
            {
                // 创建基础项目数据并保存到JSON
                var project = new Project
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = ProjectName.Trim(),
                    Description = ProjectDescription?.Trim() ?? string.Empty,
                    LocalPath = ProjectPath.Trim(),
                    WorkingDirectory = ProjectPath.Trim(),
                    Framework = SelectedFramework,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now,
                    Tags = new List<string>()
                };

                // 保存到JSON - 框架不可更改除非重新新增项目
                var saveSuccess = await _projectService.SaveProjectAsync(project);
                
                if (saveSuccess)
                {
                    CreatedProject = project;
                    ProjectCreated?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // TODO: 显示错误消息
                    System.Diagnostics.Debug.WriteLine("保存项目失败");
                }
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息
                System.Diagnostics.Debug.WriteLine($"创建项目失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogCancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}
