using System;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProjectManager.Models;
using System.Windows;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class EditEnvironmentVariableViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _variableName = string.Empty;

        [ObservableProperty]
        private string _variableValue = string.Empty;

        [ObservableProperty]
        private bool _isSystemVariable;

        [ObservableProperty]
        private bool _canEditName;

        private readonly SystemEnvironmentVariable _originalVariable;

        public EditEnvironmentVariableViewModel(SystemEnvironmentVariable variable, bool isSystemVariable, bool isNewVariable = false)
        {
            _originalVariable = variable;
            _variableName = variable.Name;
            _variableValue = variable.Value;
            _isSystemVariable = isSystemVariable;
            _canEditName = isNewVariable; // 只有新建变量时才允许编辑变量名
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "选择文件夹",
                    Filter = "文件夹|*.*",
                    FileName = "选择文件夹", // 默认文件名留空
                    CheckFileExists = false,
                    CheckPathExists = true,
                    ValidateNames = false
                };

                // 设置初始目录
                if (!string.IsNullOrEmpty(VariableValue) && Directory.Exists(VariableValue))
                {
                    dialog.InitialDirectory = VariableValue;
                }
                else if (!string.IsNullOrEmpty(VariableValue))
                {
                    // 如果VariableValue包含多个路径，使用第一个存在的路径
                    var paths = VariableValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var path in paths)
                    {
                        if (Directory.Exists(path))
                        {
                            dialog.InitialDirectory = path;
                            break;
                        }
                    }
                }

                if (dialog.ShowDialog() == true)
                {
                    // 获取选择的文件夹路径 - 使用Path.GetDirectoryName获取文件夹路径
                    string? selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        if (string.IsNullOrEmpty(VariableValue))
                        {
                            VariableValue = selectedPath;
                        }
                        else
                        {
                            // 如果已有值，添加到末尾
                            var paths = VariableValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
                            if (!paths.Contains(selectedPath))
                            {
                                VariableValue = string.Join(";", paths.Append(selectedPath));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"浏览文件夹时出错: {ex.Message}");
            }
        }

        [RelayCommand]
        private void BrowseFile()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "选择文件",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    if (string.IsNullOrEmpty(VariableValue))
                    {
                        VariableValue = dialog.FileName;
                    }
                    else
                    {
                        // 如果已有值，添加到末尾
                        var paths = VariableValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        if (!paths.Contains(dialog.FileName))
                        {
                            VariableValue = string.Join(";", paths.Append(dialog.FileName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"浏览文件时出错: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Save()
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(VariableName))
                {
                    throw new ArgumentException("变量名不能为空");
                }

                if (string.IsNullOrWhiteSpace(VariableValue))
                {
                    throw new ArgumentException("变量值不能为空");
                }

                // 更新原始变量
                _originalVariable.Name = VariableName.Trim();
                _originalVariable.Value = VariableValue.Trim();

                // 关闭窗口并返回成功
                if (Application.Current?.MainWindow != null)
                {
                    foreach (Window window in Application.Current.MainWindow.OwnedWindows)
                    {
                        if (window is Views.Dialogs.EditEnvironmentVariableWindow dialog)
                        {
                            dialog.DialogResult = true;
                            dialog.Close();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存环境变量时出错: {ex.Message}");
            }
        }
    }
}