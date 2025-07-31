using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class ProjectEditDialogViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private readonly IProjectDetectionService _detectionService;
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

        [ObservableProperty]
        private string _detectionInfo = string.Empty;

        [ObservableProperty]
        private bool _isDetecting = false;

        [ObservableProperty]
        private ObservableCollection<ProjectDetectionResult> _detectionCandidates = new();

        public event EventHandler<Project>? ProjectSaved;
        public event EventHandler<string>? ProjectDeleted;
        public event EventHandler? DialogCancelled;

        public ProjectEditDialogViewModel(IProjectService projectService, IProjectDetectionService detectionService)
        {
            _projectService = projectService;
            _detectionService = detectionService;
            
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

        private async void OnLocalPathChanged()
        {
            if (!string.IsNullOrEmpty(LocalPath) && string.IsNullOrEmpty(Framework))
            {
                try
                {
                    // 使用增强的项目检测服务
                    var detectionResult = await _detectionService.DetectProjectTypeAsync(LocalPath);
                    
                    if (detectionResult.ConfidenceLevel > 0.3 && detectionResult.DetectedFramework != "其他")
                    {
                        Framework = detectionResult.DetectedFramework;
                        
                        // 自动填充建议的信息
                        if (string.IsNullOrEmpty(ProjectName) && !string.IsNullOrEmpty(detectionResult.SuggestedName))
                        {
                            ProjectName = detectionResult.SuggestedName;
                        }
                        
                        if (string.IsNullOrEmpty(ProjectDescription) && !string.IsNullOrEmpty(detectionResult.SuggestedDescription))
                        {
                            ProjectDescription = detectionResult.SuggestedDescription;
                        }
                        
                        if (string.IsNullOrEmpty(StartCommand) && !string.IsNullOrEmpty(detectionResult.SuggestedStartCommand))
                        {
                            StartCommand = detectionResult.SuggestedStartCommand;
                        }
                        
                        if ((string.IsNullOrEmpty(Port) || Port == "0") && detectionResult.SuggestedPort > 0)
                        {
                            Port = detectionResult.SuggestedPort.ToString();
                        }
                        
                        if (string.IsNullOrEmpty(TagsString) && detectionResult.SuggestedTags.Any())
                        {
                            TagsString = string.Join(", ", detectionResult.SuggestedTags);
                        }
                        
                        // 显示检测信息（可选）
                        DetectionInfo = $"检测结果: {detectionResult.DetectedFramework} (置信度: {detectionResult.ConfidenceLevel:P1})";
                        if (!string.IsNullOrEmpty(detectionResult.DetectionReason))
                        {
                            DetectionInfo += $"\n原因: {detectionResult.DetectionReason}";
                        }
                    }
                    else
                    {
                        // 回退到原有检测逻辑
                        var detectedFramework = FrameworkConfigService.DetectFramework(LocalPath);
                        if (detectedFramework != "其他")
                        {
                            Framework = detectedFramework;
                        }
                        DetectionInfo = "使用基础检测逻辑";
                    }
                }
                catch (Exception ex)
                {
                    // 如果检测失败，回退到原有逻辑
                    var detectedFramework = FrameworkConfigService.DetectFramework(LocalPath);
                    if (detectedFramework != "其他")
                    {
                        Framework = detectedFramework;
                    }
                    DetectionInfo = $"检测出错，使用基础检测: {ex.Message}";
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
                PythonEnvironment = project.PythonEnvironment ?? string.Empty;
                Framework = project.Framework ?? string.Empty;
                Author = project.Author ?? string.Empty;
                Version = project.Version ?? "1.0.0";
                Port = project.Port.ToString();
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
                PythonEnvironment = string.Empty;
                Framework = string.Empty;
                Author = string.Empty;
                Version = "1.0.0";
                Port = "0";
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("项目名称"))
            {
                await ShowErrorMessage(ex.Message);
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

        [RelayCommand]
        private async Task DetectProjectType()
        {
            if (string.IsNullOrEmpty(LocalPath) || !Directory.Exists(LocalPath))
            {
                DetectionInfo = "请先选择有效的项目路径";
                return;
            }

            try
            {
                IsDetecting = true;
                DetectionInfo = "正在检测项目类型...";
                
                // 获取多个检测候选项
                var candidates = await _detectionService.GetMultipleCandidatesAsync(LocalPath);
                DetectionCandidates.Clear();
                
                foreach (var candidate in candidates.Take(5)) // 最多显示5个候选项
                {
                    DetectionCandidates.Add(candidate);
                }

                if (candidates.Any())
                {
                    var bestCandidate = candidates.First();
                    DetectionInfo = $"检测到 {candidates.Count} 个可能的项目类型，最佳匹配: {bestCandidate.DetectedFramework} (置信度: {bestCandidate.ConfidenceLevel:P1})";
                }
                else
                {
                    DetectionInfo = "未能检测到已知的项目类型";
                }
            }
            catch (Exception ex)
            {
                DetectionInfo = $"检测失败: {ex.Message}";
            }
            finally
            {
                IsDetecting = false;
            }
        }

        [RelayCommand]
        private void ApplyDetectionResult(ProjectDetectionResult? detectionResult)
        {
            if (detectionResult == null) return;

            Framework = detectionResult.DetectedFramework;
            
            if (string.IsNullOrEmpty(ProjectName) && !string.IsNullOrEmpty(detectionResult.SuggestedName))
            {
                ProjectName = detectionResult.SuggestedName;
            }
            
            if (string.IsNullOrEmpty(ProjectDescription) && !string.IsNullOrEmpty(detectionResult.SuggestedDescription))
            {
                ProjectDescription = detectionResult.SuggestedDescription;
            }
            
            if (string.IsNullOrEmpty(StartCommand) && !string.IsNullOrEmpty(detectionResult.SuggestedStartCommand))
            {
                StartCommand = detectionResult.SuggestedStartCommand;
            }
            
            if ((string.IsNullOrEmpty(Port) || Port == "0") && detectionResult.SuggestedPort > 0)
            {
                Port = detectionResult.SuggestedPort.ToString();
            }
            
            if (string.IsNullOrEmpty(TagsString) && detectionResult.SuggestedTags.Any())
            {
                TagsString = string.Join(", ", detectionResult.SuggestedTags);
            }

            DetectionInfo = $"已应用 {detectionResult.DetectedFramework} 的检测结果";
        }

        private async Task ShowErrorMessage(string message)
        {
            var messageBox = new MessageBox
            {
                Title = "错误",
                Content = message,
                PrimaryButtonText = "确定"
            };
            await messageBox.ShowDialogAsync();
        }
    }
}
