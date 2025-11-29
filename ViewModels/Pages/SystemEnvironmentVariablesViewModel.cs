using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProjectManager.Models;
using ProjectManager.Views.Dialogs;
using Wpf.Ui.Controls;
using ProjectManager.Helpers;
using ProjectManager.Services;

namespace ProjectManager.ViewModels.Pages
{
    public partial class SystemEnvironmentVariablesViewModel : ObservableObject
    {
        private readonly EnvironmentVariableService _envService;
        private readonly IErrorDisplayService _errorDisplayService;
        private bool _isUpdatingSelection; // 防止递归更新的标志

        [ObservableProperty]
        private ObservableCollection<SystemEnvironmentVariable> _userVariables = new();

        [ObservableProperty]
        private ObservableCollection<SystemEnvironmentVariable> _systemVariables = new();

        [ObservableProperty]
        private SystemEnvironmentVariable? _selectedUserVariable;

        [ObservableProperty]
        private SystemEnvironmentVariable? _selectedSystemVariable;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _selectedFilterIndex = 0; // 0: 全部, 1: 用户变量, 2: 系统变量

        [ObservableProperty]
        private int _userVariablesCount = 0;

        [ObservableProperty]
        private int _systemVariablesCount = 0;

        [ObservableProperty]
        private ICollectionView? _filteredUserVariables;

        [ObservableProperty]
        private ICollectionView? _filteredSystemVariables;

        public bool HasUserVariables => UserVariables.Count > 0;
        public bool HasSystemVariables => SystemVariables.Count > 0;
        public bool HasSelectedUserVariable => SelectedUserVariable != null;
        public bool HasSelectedSystemVariable => SelectedSystemVariable != null;
        public bool HasSelection => SelectedUserVariable != null || SelectedSystemVariable != null;

        public SystemEnvironmentVariablesViewModel(IErrorDisplayService errorDisplayService)
        {
            _envService = new EnvironmentVariableService();
            _errorDisplayService = errorDisplayService;
            
            FilteredUserVariables = CollectionViewSource.GetDefaultView(UserVariables);
            FilteredUserVariables.Filter = FilterUserVariables;

            FilteredSystemVariables = CollectionViewSource.GetDefaultView(SystemVariables);
            FilteredSystemVariables.Filter = FilterSystemVariables;

            LoadEnvironmentVariables();
        }

        private void LoadEnvironmentVariables()
        {
            try
            {
                UserVariables.Clear();
                SystemVariables.Clear();

                // 加载用户环境变量
                var userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
                foreach (System.Collections.DictionaryEntry entry in userVars)
                {
                    UserVariables.Add(new SystemEnvironmentVariable(
                        entry.Key.ToString() ?? string.Empty,
                        entry.Value?.ToString() ?? string.Empty,
                        false));
                }

                // 加载系统环境变量
                var systemVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
                foreach (System.Collections.DictionaryEntry entry in systemVars)
                {
                    SystemVariables.Add(new SystemEnvironmentVariable(
                        entry.Key.ToString() ?? string.Empty,
                        entry.Value?.ToString() ?? string.Empty,
                        true));
                }

                // 更新计数
                UserVariablesCount = UserVariables.Count;
                SystemVariablesCount = SystemVariables.Count;

                OnPropertyChanged(nameof(HasUserVariables));
                OnPropertyChanged(nameof(HasSystemVariables));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载环境变量时出错: {ex.Message}");
            }
        }

        private bool FilterUserVariables(object obj)
        {
            if (obj is not SystemEnvironmentVariable variable) return false;
            if (variable.IsSystemVariable) return false;

            // 根据筛选条件显示
            if (SelectedFilterIndex == 2) return false; // 只显示系统变量

            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                return variable.Name.ToLower().Contains(searchLower) ||
                       variable.Value.ToLower().Contains(searchLower);
            }

            return true;
        }

        private bool FilterSystemVariables(object obj)
        {
            if (obj is not SystemEnvironmentVariable variable) return false;
            if (!variable.IsSystemVariable) return false;

            // 根据筛选条件显示
            if (SelectedFilterIndex == 1) return false; // 只显示用户变量

            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                return variable.Name.ToLower().Contains(searchLower) ||
                       variable.Value.ToLower().Contains(searchLower);
            }

            return true;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredUserVariables?.Refresh();
            FilteredSystemVariables?.Refresh();
        }

        partial void OnSelectedFilterIndexChanged(int value)
        {
            FilteredUserVariables?.Refresh();
            FilteredSystemVariables?.Refresh();
        }

        partial void OnSelectedUserVariableChanged(SystemEnvironmentVariable? value)
        {
            if (_isUpdatingSelection) return; // 防止递归
            
            // 实现互斥选择：如果用户变量被选中，清除系统变量的选择
            if (value != null && SelectedSystemVariable != null)
            {
                _isUpdatingSelection = true;
                SelectedSystemVariable = null;
                _isUpdatingSelection = false;
            }
            
            OnPropertyChanged(nameof(HasSelectedUserVariable));
            OnPropertyChanged(nameof(HasSelection));
        }

        partial void OnSelectedSystemVariableChanged(SystemEnvironmentVariable? value)
        {
            if (_isUpdatingSelection) return; // 防止递归
            
            // 实现互斥选择：如果系统变量被选中，清除用户变量的选择
            if (value != null && SelectedUserVariable != null)
            {
                _isUpdatingSelection = true;
                SelectedUserVariable = null;
                _isUpdatingSelection = false;
            }
            
            OnPropertyChanged(nameof(HasSelectedSystemVariable));
            OnPropertyChanged(nameof(HasSelection));
        }

        [RelayCommand]
        private async Task DeleteUserVariable()
        {
            if (SelectedUserVariable == null) return;

            var confirmed = await _errorDisplayService.ShowConfirmationAsync(
                $"确定要删除用户环境变量 '{SelectedUserVariable.Name}' 吗？",
                "确认删除");

            if (confirmed)
            {
                if (_envService.DeleteVariable(SelectedUserVariable.Name, false))
                {
                    LoadEnvironmentVariables();
                }
                else
                {
                    await _errorDisplayService.ShowErrorAsync("删除用户环境变量失败");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteSystemVariable()
        {
            if (SelectedSystemVariable == null) return;

            var confirmed = await _errorDisplayService.ShowConfirmationAsync(
                $"确定要删除系统环境变量 '{SelectedSystemVariable.Name}' 吗？\n\n注意：此操作需要管理员权限。",
                "确认删除");

            if (confirmed)
            {
                await DeleteSystemVariableWithUac(SelectedSystemVariable.Name);
            }
        }

        private async Task DeleteSystemVariableWithUac(string name)
        {
            if (_envService.HasAdminPrivileges())
            {
                // 当前已有管理员权限，直接删除
                if (_envService.DeleteVariable(name, true))
                {
                    LoadEnvironmentVariables();
                }
                else
                {
                    _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync("删除系统环境变量失败"));
                }
            }
            else
            {
                // 没有管理员权限，直接使用UAC提权
                if (_envService.DeleteVariable(name, true))
                {
                    LoadEnvironmentVariables();
                }
                else
                {
                    _ = Task.Run(async () => await _errorDisplayService.ShowInfoAsync("删除系统环境变量失败，用户取消了提权或权限不足。", "提示"));
                }
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadEnvironmentVariables();
        }

        [RelayCommand]
        private async Task EditSelected()
        {
            if (SelectedUserVariable != null)
            {
                await EditVariable(SelectedUserVariable, false);
            }
            else if (SelectedSystemVariable != null)
            {
                await EditVariable(SelectedSystemVariable, true);
            }
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            if (SelectedUserVariable != null)
            {
                await DeleteUserVariable();
            }
            else if (SelectedSystemVariable != null)
            {
                await DeleteSystemVariable();
            }
        }

        [RelayCommand]
        private async Task AddUserVariable()
        {
            var newVariable = new SystemEnvironmentVariable("新变量", "", false);
            var editWindow = new Views.Dialogs.EditEnvironmentVariableWindow();
            editWindow.Owner = Application.Current.MainWindow;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var viewModel = new ViewModels.Dialogs.EditEnvironmentVariableViewModel(newVariable, false, true); // true表示新建变量
            editWindow.DataContext = viewModel;

            if (editWindow.ShowDialog() == true)
            {
                if (_envService.SetUserVariable(viewModel.VariableName, viewModel.VariableValue))
                {
                    LoadEnvironmentVariables();
                }
                else
                {
                    await _errorDisplayService.ShowErrorAsync("添加用户环境变量失败");
                }
            }
        }

        [RelayCommand]
        private async Task AddSystemVariable()
        {
            var newVariable = new SystemEnvironmentVariable("新变量", "", true);
            var editWindow = new Views.Dialogs.EditEnvironmentVariableWindow();
            editWindow.Owner = Application.Current.MainWindow;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var viewModel = new ViewModels.Dialogs.EditEnvironmentVariableViewModel(newVariable, true, true); // true表示新建变量
            editWindow.DataContext = viewModel;

            if (editWindow.ShowDialog() == true)
            {
                await SaveSystemVariableWithUac(viewModel.VariableName, viewModel.VariableValue);
            }
        }

        [RelayCommand]
        private async Task EditUserVariable()
        {
            if (SelectedUserVariable == null) return;
            await EditVariable(SelectedUserVariable, false);
        }

        [RelayCommand]
        private async Task EditSystemVariable()
        {
            if (SelectedSystemVariable == null) return;
            await EditVariable(SelectedSystemVariable, true);
        }

        private async Task EditVariable(SystemEnvironmentVariable variable, bool isSystemVariable)
        {
            try
            {
                // 检查是否是PATH变量，如果是则使用专门的Path编辑器
                if (string.Equals(variable.Name, "PATH", StringComparison.OrdinalIgnoreCase))
                {
                    // 使用专门的Path编辑器
                    var pathEditorViewModel = new ViewModels.Dialogs.PathEditorViewModel(variable.Value, isSystemVariable);
                    var pathEditor = new Views.Dialogs.PathEditorWindow(pathEditorViewModel);
                    pathEditor.Owner = Application.Current.MainWindow;
                    pathEditor.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    if (pathEditor.ShowDialog() == true)
                    {
                        // 保存PATH变量的更改
                        await SaveEnvironmentVariable(variable.Name, pathEditorViewModel.GetResultPath(), isSystemVariable);
                    }
                }
                else
                {
                    // 使用标准的环境变量编辑窗口
                    var editWindow = new Views.Dialogs.EditEnvironmentVariableWindow();
                    editWindow.Owner = Application.Current.MainWindow;
                    editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    var viewModel = new ViewModels.Dialogs.EditEnvironmentVariableViewModel(variable, isSystemVariable);
                    editWindow.DataContext = viewModel;

                    if (editWindow.ShowDialog() == true)
                    {
                        // 保存更改 - 根据是否需要管理员权限采用不同策略
                        await SaveEnvironmentVariable(viewModel.VariableName, viewModel.VariableValue, isSystemVariable);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"编辑环境变量时出错: {ex.Message}");
            }
        }

        private async Task SaveEnvironmentVariable(string name, string value, bool isSystemVariable)
        {
            if (isSystemVariable)
            {
                // 系统环境变量需要管理员权限
                await SaveSystemVariableWithUac(name, value);
            }
            else
            {
                // 用户环境变量不需要特殊权限
                if (_envService.SetUserVariable(name, value))
                {
                    LoadEnvironmentVariables();
                }
                else
                {
                    _ = Task.Run(async () => await _errorDisplayService.ShowErrorAsync("保存用户环境变量失败"));
                }
            }
        }

        private async Task SaveSystemVariableWithUac(string name, string value)
        {
            if (_envService.HasAdminPrivileges())
            {
                // 当前已有管理员权限，直接保存
                if (_envService.SetSystemVariable(name, value))
                {
                    LoadEnvironmentVariables();
                }
                else
                {
                    await _errorDisplayService.ShowErrorAsync("保存系统环境变量失败");
                }
            }
            else
            {
                // 没有管理员权限，直接使用UAC提权（不显示确认对话框）
                if (_envService.SetSystemVariableWithUac(name, value))
                {
                    LoadEnvironmentVariables();
                }
                else
                {
                    await _errorDisplayService.ShowInfoAsync("设置系统环境变量失败，用户取消了提权或权限不足。", "提示");
                }
            }
        }
    }
}