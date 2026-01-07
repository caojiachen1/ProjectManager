using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProjectManager.Views.Dialogs;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class PathEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<PathItem> _pathItems = new();

        [ObservableProperty]
        private PathItem? _selectedPathItem;

        [ObservableProperty]
        private string _editText = string.Empty;

        [ObservableProperty]
        private bool _isSystemVariable;

        [ObservableProperty]
        private bool _isListMode = true; // 默认是列表模式

        private string _originalPath = string.Empty;
        private bool _isInternalUpdate;

        public event EventHandler<bool>? CloseRequested;

        public PathEditorViewModel(string path, bool isSystemVariable)
        {
            _isSystemVariable = isSystemVariable;
            _originalPath = path;
            LoadPathItems(path);
            UpdateEditText();
        }

        private void LoadPathItems(string path)
        {
            PathItems.Clear();
            if (string.IsNullOrEmpty(path))
                return;

            var paths = path.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pathItem in paths)
            {
                PathItems.Add(new PathItem
                {
                    Path = pathItem,
                    Status = GetPathStatus(pathItem)
                });
            }
        }

        private PathStatus GetPathStatus(string path)
        {
            if (string.IsNullOrEmpty(path))
                return PathStatus.Invalid;

            try
            {
                if (Directory.Exists(path))
                    return PathStatus.Valid;
                if (File.Exists(path))
                    return PathStatus.Valid;
                return PathStatus.NotFound;
            }
            catch
            {
                return PathStatus.Invalid;
            }
        }

        private void UpdateEditText()
        {
            _isInternalUpdate = true;
            try
            {
                EditText = string.Join(";", PathItems.Select(p => p.Path));
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        [RelayCommand]
        private void New()
        {
            var dialog = new PathItemEditWindow();
            var viewModel = new PathItemEditViewModel();
            dialog.DataContext = viewModel;
            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var newItem = new PathItem
                {
                    Path = viewModel.Path,
                    Status = GetPathStatus(viewModel.Path)
                };
                PathItems.Add(newItem);
                UpdateEditText();
            }
        }

        [RelayCommand]
        private void Edit()
        {
            if (SelectedPathItem == null) return;

            var dialog = new PathItemEditWindow();
            var viewModel = new PathItemEditViewModel { Path = SelectedPathItem.Path };
            dialog.DataContext = viewModel;
            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var result = dialog.ShowDialog();
            System.Diagnostics.Debug.WriteLine($"PathItemEditWindow.ShowDialog() returned: {result}");
            
            if (result == true)
            {
                System.Diagnostics.Debug.WriteLine($"Updating path from: {SelectedPathItem.Path} to: {viewModel.Path}");
                SelectedPathItem.Path = viewModel.Path;
                SelectedPathItem.Status = GetPathStatus(viewModel.Path);
                UpdateEditText();
                System.Diagnostics.Debug.WriteLine($"Path updated successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Edit operation cancelled, path remains: {SelectedPathItem.Path}");
            }
        }

        [RelayCommand]
        private void Browse()
        {
            if (SelectedPathItem == null) return;

            var dialog = new OpenFileDialog
            {
                Title = "选择文件",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            if (!string.IsNullOrEmpty(SelectedPathItem.Path) && File.Exists(SelectedPathItem.Path))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(SelectedPathItem.Path);
                dialog.FileName = Path.GetFileName(SelectedPathItem.Path);
            }
            else if (!string.IsNullOrEmpty(SelectedPathItem.Path) && Directory.Exists(SelectedPathItem.Path))
            {
                dialog.InitialDirectory = SelectedPathItem.Path;
            }

            if (dialog.ShowDialog() == true)
            {
                SelectedPathItem.Path = dialog.FileName;
                SelectedPathItem.Status = GetPathStatus(dialog.FileName);
                UpdateEditText();
            }
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedPathItem == null) return;

            PathItems.Remove(SelectedPathItem);
            UpdateEditText();
        }

        [RelayCommand]
        private void MoveUp()
        {
            if (SelectedPathItem == null) return;

            var item = SelectedPathItem;
            var index = PathItems.IndexOf(item);
            if (index > 0)
            {
                PathItems.Move(index, index - 1);
                SelectedPathItem = item;
                UpdateEditText();
            }
        }

        [RelayCommand]
        private void MoveDown()
        {
            if (SelectedPathItem == null) return;

            var item = SelectedPathItem;
            var index = PathItems.IndexOf(item);
            if (index < PathItems.Count - 1)
            {
                PathItems.Move(index, index + 1);
                SelectedPathItem = item;
                UpdateEditText();
            }
        }

        partial void OnEditTextChanged(string value)
        {
            if (_isInternalUpdate) return;

            if (string.IsNullOrWhiteSpace(value))
            {
                PathItems.Clear();
                return;
            }

            var paths = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            PathItems.Clear();
            foreach (var path in paths)
            {
                PathItems.Add(new PathItem
                {
                    Path = path.Trim(),
                    Status = GetPathStatus(path.Trim())
                });
            }
        }

        [RelayCommand]
        private void Save()
        {
            var result = string.Join(";", PathItems.Select(p => p.Path));
            CloseRequested?.Invoke(this, true);
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(this, false);
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            if (IsListMode)
            {
                // 切换到文本编辑模式
                var textEditWindow = new PathTextEditWindow(new PathTextEditViewModel(GetResultPath()));
                textEditWindow.Owner = Application.Current.MainWindow;
                textEditWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (textEditWindow.ShowDialog() == true)
                {
                    if (textEditWindow.DataContext is PathTextEditViewModel viewModel)
                    {
                        // 从文本模式返回，更新路径
                        LoadPathItems(viewModel.PathText);
                    }
                }
            }
        }

        // 当SelectedPathItem改变时，通知相关的计算属性更新
        partial void OnSelectedPathItemChanged(PathItem? value)
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanMoveUp));
            OnPropertyChanged(nameof(CanMoveDown));
        }

        // 当PathItems集合改变时，通知相关的计算属性更新
        partial void OnPathItemsChanged(ObservableCollection<PathItem> value)
        {
            OnPropertyChanged(nameof(CanMoveUp));
            OnPropertyChanged(nameof(CanMoveDown));
            
            // 如果集合实现了INotifyCollectionChanged，订阅其CollectionChanged事件
            if (value != null)
            {
                value.CollectionChanged += (sender, e) =>
                {
                    OnPropertyChanged(nameof(CanMoveUp));
                    OnPropertyChanged(nameof(CanMoveDown));
                };
            }
        }

        // 计算属性
        public bool HasSelection => SelectedPathItem != null;
        public bool CanMoveUp => SelectedPathItem != null && PathItems.IndexOf(SelectedPathItem) > 0;
        public bool CanMoveDown => SelectedPathItem != null && PathItems.IndexOf(SelectedPathItem) < PathItems.Count - 1;

        // 公共方法
        public string GetResultPath()
        {
            return string.Join(";", PathItems.Select(p => p.Path));
        }
    }

    public partial class PathItem : ObservableObject
    {
        [ObservableProperty]
        private string _path = string.Empty;

        [ObservableProperty]
        private PathStatus _status;

        public string StatusIcon => Status switch
        {
            PathStatus.Valid => "CheckmarkCircle24",
            PathStatus.NotFound => "Warning24",
            PathStatus.Invalid => "ErrorCircle24",
            _ => "QuestionCircle24"
        };

        public string StatusText => Status switch
        {
            PathStatus.Valid => "有效",
            PathStatus.NotFound => "未找到",
            PathStatus.Invalid => "无效",
            _ => "未知"
        };

        public Brush StatusColor => GetStatusBrush(Status);

        private Brush GetStatusBrush(PathStatus status)
        {
            try
            {
                return status switch
                {
                    PathStatus.Valid => Application.Current?.Resources["SystemFillColorSuccessBrush"] as Brush ?? new SolidColorBrush(Colors.Green),
                    PathStatus.NotFound => Application.Current?.Resources["SystemFillColorCautionBrush"] as Brush ?? new SolidColorBrush(Colors.Orange),
                    PathStatus.Invalid => Application.Current?.Resources["SystemFillColorCriticalBrush"] as Brush ?? new SolidColorBrush(Colors.Red),
                    _ => Application.Current?.Resources["TextFillColorSecondaryBrush"] as Brush ?? new SolidColorBrush(Colors.Gray)
                };
            }
            catch
            {
                // 如果资源查找失败，使用默认颜色
                return status switch
                {
                    PathStatus.Valid => new SolidColorBrush(Colors.Green),
                    PathStatus.NotFound => new SolidColorBrush(Colors.Orange),
                    PathStatus.Invalid => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }
    }

    public enum PathStatus
    {
        Valid,
        NotFound,
        Invalid
    }
}