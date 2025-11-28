using System;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class PathItemEditViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _path = string.Empty;

        public event EventHandler<bool>? CloseRequested;

        public PathItemEditViewModel()
        {
            System.Diagnostics.Debug.WriteLine("PathItemEditViewModel constructor called");
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
                    CheckFileExists = false,
                    CheckPathExists = true,
                    ValidateNames = false
                };

                if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path))
                {
                    dialog.InitialDirectory = Path;
                }

                if (dialog.ShowDialog() == true)
                {
                    string? selectedPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        Path = selectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"浏览文件夹时出错: {ex.Message}");
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

                if (!string.IsNullOrEmpty(Path) && File.Exists(Path))
                {
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(Path);
                    dialog.FileName = System.IO.Path.GetFileName(Path);
                }
                else if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path))
                {
                    dialog.InitialDirectory = Path;
                }

                if (dialog.ShowDialog() == true)
                {
                    Path = dialog.FileName;
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
            System.Diagnostics.Debug.WriteLine($"PathItemEditViewModel.Save() called, Path: '{Path}'");
            
            if (string.IsNullOrWhiteSpace(Path))
            {
                System.Diagnostics.Debug.WriteLine("Path is empty, showing warning message");
                _ = System.Windows.MessageBox.Show("路径不能为空", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine("Path validation passed, firing CloseRequested with true");
            CloseRequested?.Invoke(this, true);
        }

        [RelayCommand]
        private void Cancel()
        {
            System.Diagnostics.Debug.WriteLine($"PathItemEditViewModel.Cancel() called");
            try
            {
                // 直接关闭，不保存任何更改
                CloseRequested?.Invoke(this, false);
                System.Diagnostics.Debug.WriteLine($"CloseRequested event fired with result: false");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Cancel command: {ex.Message}");
            }
        }
    }
}