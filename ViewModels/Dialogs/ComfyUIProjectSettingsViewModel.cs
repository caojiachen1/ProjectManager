using ProjectManager.Models;
using ProjectManager.Services;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Collections.ObjectModel;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class ComfyUIProjectSettingsViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private Project? _project;
        private bool _isUpdatingCommand = false;

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private string _projectPath = string.Empty;

        [ObservableProperty]
        private string _startCommand = "python main.py";

        [ObservableProperty]
        private int _port = 8188;

        partial void OnPortChanged(int value)
        {
            // 端口变化时立即更新启动命令
            if (!_isUpdatingCommand)
            {
                UpdateStartCommand();
            }
        }

        [ObservableProperty]
        private string _pythonPath = string.Empty;

        [ObservableProperty]
        private bool _isPythonPathValid = true;

        [ObservableProperty]
        private ObservableCollection<string> _startCommandSuggestions = new ObservableCollection<string>();

        partial void OnPythonPathChanged(string value)
        {
            // 验证Python路径有效性
            var isValid = IsValidPythonPath(value);
            IsPythonPathValid = isValid;
            
            // Python路径变化时验证有效性，只有有效路径才更新启动命令
            if (!_isUpdatingCommand && isValid)
            {
                UpdateStartCommand();
            }
            
            // 如果路径无效，仍然需要更新建议（使用默认python）
            if (!_isUpdatingCommand && !isValid)
            {
                UpdateStartCommandSuggestions();
            }
        }

        private bool IsValidPythonPath(string path)
        {
            // 如果路径为空，认为是有效的（使用默认python命令）
            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            try
            {
                // 检查路径格式是否有效
                var fullPath = Path.GetFullPath(path);
                
                // 检查文件是否存在
                if (!File.Exists(fullPath))
                {
                    return false;
                }

                // 检查文件名是否为python.exe（不区分大小写）
                var fileName = Path.GetFileName(fullPath);
                if (!fileName.Equals("python.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                // 如果路径格式无效，返回false
                return false;
            }
        }

        [ObservableProperty]
        private bool _listenAllInterfaces = false;

        partial void OnListenAllInterfacesChanged(bool value)
        {
            // 监听模式变化时立即更新启动命令
            if (!_isUpdatingCommand)
            {
                UpdateStartCommand();
            }
        }

        [ObservableProperty]
        private bool _lowVramMode = false;

        partial void OnLowVramModeChanged(bool value)
        {
            // 低显存模式变化时立即更新启动命令
            if (!_isUpdatingCommand)
            {
                UpdateStartCommand();
            }
        }

        [ObservableProperty]
        private bool _cpuMode = false;

        partial void OnCpuModeChanged(bool value)
        {
            // CPU模式变化时立即更新启动命令
            if (!_isUpdatingCommand)
            {
                UpdateStartCommand();
            }
        }

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
            
            // 确保有默认的启动命令
            if (string.IsNullOrEmpty(StartCommand))
            {
                StartCommand = "python main.py";
            }
            
            // 初始化启动命令建议
            UpdateStartCommandSuggestions();
        }

        private void UpdateStartCommandSuggestions()
        {
            var pythonCommand = string.IsNullOrEmpty(PythonPath) || !IsValidPythonPath(PythonPath) ? "python" : $"\"{PythonPath}\"";
            var currentCommand = StartCommand;
            
            StartCommandSuggestions.Clear();
            StartCommandSuggestions.Add($"{pythonCommand} main.py");
            StartCommandSuggestions.Add($"{pythonCommand} main.py --listen");
            StartCommandSuggestions.Add($"{pythonCommand} main.py --port 8188");
            StartCommandSuggestions.Add($"{pythonCommand} main.py --listen --port 8188");
            StartCommandSuggestions.Add($"{pythonCommand} main.py --cpu");
            StartCommandSuggestions.Add($"{pythonCommand} main.py --directml");
            StartCommandSuggestions.Add($"{pythonCommand} main.py --lowvram");
            StartCommandSuggestions.Add($"{pythonCommand} main.py --listen --lowvram");
            
            // 如果当前命令不在建议列表中，将其添加到列表首位
            if (!string.IsNullOrEmpty(currentCommand) && !StartCommandSuggestions.Contains(currentCommand))
            {
                StartCommandSuggestions.Insert(0, currentCommand);
            }
        }

        private void UpdateStartCommand()
        {
            _isUpdatingCommand = true;
            try
            {
                var currentCommand = StartCommand ?? "python main.py";
                
                // 使用正则表达式更新Python路径，只有有效路径才使用
                var pythonCommand = string.IsNullOrEmpty(PythonPath) || !IsValidPythonPath(PythonPath) ? "python" : $"\"{PythonPath}\"";
                
                // 更新Python可执行文件路径 (支持带引号和不带引号的路径)
                var pythonPattern = @"^(""?[^""]*?(?:python(?:\.exe)?))""?(\s+main\.py.*)$";
                var pythonReplacement = $"{pythonCommand}$2";
                currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, pythonPattern, pythonReplacement, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // 更新端口设置
                if (Port != 8188)
                {
                    // 如果已经有端口参数，更新它
                    var portPattern = @"--port\s+\d+";
                    if (System.Text.RegularExpressions.Regex.IsMatch(currentCommand, portPattern))
                    {
                        currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, portPattern, $"--port {Port}");
                    }
                    else
                    {
                        // 如果没有端口参数，添加它
                        currentCommand += $" --port {Port}";
                    }
                }
                else
                {
                    // 如果端口是默认值8188，移除端口参数
                    var portPattern = @"\s*--port\s+8188\b";
                    currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, portPattern, "");
                }
                
                // 更新--listen参数
                var listenPattern = @"\s*--listen\b";
                if (ListenAllInterfaces)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(currentCommand, listenPattern))
                    {
                        currentCommand += " --listen";
                    }
                }
                else
                {
                    currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, listenPattern, "");
                }
                
                // 更新CPU模式参数
                var cpuPattern = @"\s*--cpu\b";
                if (CpuMode)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(currentCommand, cpuPattern))
                    {
                        currentCommand += " --cpu";
                    }
                    // 如果启用CPU模式，移除低显存模式
                    var lowvramPattern = @"\s*--lowvram\b";
                    currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, lowvramPattern, "");
                }
                else
                {
                    currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, cpuPattern, "");
                }
                
                // 更新低显存模式参数
                var lowvramPattern2 = @"\s*--lowvram\b";
                if (LowVramMode && !CpuMode)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(currentCommand, lowvramPattern2))
                    {
                        currentCommand += " --lowvram";
                    }
                }
                else if (!CpuMode)  // 只有在不是CPU模式时才移除
                {
                    currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, lowvramPattern2, "");
                }
                
                // 清理多余的空格
                currentCommand = System.Text.RegularExpressions.Regex.Replace(currentCommand, @"\s+", " ").Trim();
                
                StartCommand = currentCommand;
            }
            finally
            {
                _isUpdatingCommand = false;
            }
            
            // 在命令更新完成后更新建议
            UpdateStartCommandSuggestions();
        }

        public void LoadProject(Project project)
        {
            _isUpdatingCommand = true;
            try
            {
                _project = project;
                
                ProjectName = project.Name ?? string.Empty;
                ProjectPath = project.LocalPath ?? string.Empty;
                StartCommand = project.StartCommand ?? "python main.py";
                TagsString = project.Tags != null ? string.Join(", ", project.Tags) : "AI绘画,图像生成,工作流,节点编辑";
                
                // 解析ComfyUI特定的启动参数
                ParseStartCommand(project.StartCommand ?? "python main.py");
                
                // 更新启动命令建议
                UpdateStartCommandSuggestions();
            }
            finally
            {
                _isUpdatingCommand = false;
            }
        }

        private void ParseStartCommand(string command)
        {
            // 解析--listen参数
            ListenAllInterfaces = System.Text.RegularExpressions.Regex.IsMatch(command, @"\b--listen\b");
            
            // 解析--cpu参数
            CpuMode = System.Text.RegularExpressions.Regex.IsMatch(command, @"\b--cpu\b");
            
            // 解析--lowvram参数
            LowVramMode = System.Text.RegularExpressions.Regex.IsMatch(command, @"\b--lowvram\b");
            
            // 解析端口
            var portMatch = System.Text.RegularExpressions.Regex.Match(command, @"--port\s+(\d+)");
            if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out int port))
            {
                Port = port;
            }
            else
            {
                Port = 8188; // 默认端口
            }
            
            // 解析Python路径
            var pythonMatch = System.Text.RegularExpressions.Regex.Match(command, @"^""?([^""]+?)""?\s+main\.py", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (pythonMatch.Success)
            {
                var extractedPath = pythonMatch.Groups[1].Value.Trim('"');
                // 如果路径不是简单的"python"，则认为是指定的Python路径
                if (!extractedPath.Equals("python", StringComparison.OrdinalIgnoreCase))
                {
                    PythonPath = extractedPath;
                }
                else
                {
                    PythonPath = string.Empty;
                }
            }
            else
            {
                PythonPath = string.Empty;
            }
            
            // 更新Python路径有效性状态
            IsPythonPathValid = IsValidPythonPath(PythonPath);
        }

        [RelayCommand]
        private void BrowsePythonPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择Python可执行文件",
                Filter = "Python可执行文件 (python.exe)|python.exe",
                InitialDirectory = string.IsNullOrEmpty(PythonPath) ? 
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : 
                    Path.GetDirectoryName(PythonPath),
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                // 验证选择的文件是否为python.exe
                if (Path.GetFileName(dialog.FileName).ToLower() == "python.exe")
                {
                    PythonPath = dialog.FileName;
                }
                else
                {
                    // TODO: 可以添加错误提示，但这里由于过滤器限制，理论上不会发生
                    System.Diagnostics.Debug.WriteLine("只能选择python.exe文件");
                }
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
