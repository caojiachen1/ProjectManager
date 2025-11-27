using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace ProjectManager.ViewModels.Pages
{
    public partial class SystemEnvironmentVariablesViewModel : ObservableObject
    {
        private readonly EnvironmentVariableService _envService;

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

        public SystemEnvironmentVariablesViewModel()
        {
            _envService = new EnvironmentVariableService();
            
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
            OnPropertyChanged(nameof(HasSelectedUserVariable));
        }

        partial void OnSelectedSystemVariableChanged(SystemEnvironmentVariable? value)
        {
            OnPropertyChanged(nameof(HasSelectedSystemVariable));
        }

        [RelayCommand]
        private void DeleteUserVariable()
        {
            if (SelectedUserVariable == null) return;

            MessageBoxResult result = MessageBox.Show(
                $"确定要删除用户环境变量 '{SelectedUserVariable.Name}' 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (_envService.DeleteVariable(SelectedUserVariable.Name, false))
                {
                    LoadEnvironmentVariables();
                    MessageBox.Show("用户环境变量已删除！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("删除用户环境变量失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void DeleteSystemVariable()
        {
            if (SelectedSystemVariable == null) return;

            MessageBoxResult result = MessageBox.Show(
                $"确定要删除系统环境变量 '{SelectedSystemVariable.Name}' 吗？\n\n注意：此操作需要管理员权限。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteSystemVariableWithUac(SelectedSystemVariable.Name);
            }
        }

        private void DeleteSystemVariableWithUac(string name)
        {
            if (_envService.HasAdminPrivileges())
            {
                // 当前已有管理员权限，直接删除
                if (_envService.DeleteVariable(name, true))
                {
                    LoadEnvironmentVariables();
                    MessageBox.Show("系统环境变量已删除！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("删除系统环境变量失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // 没有管理员权限，直接使用UAC提权
                if (_envService.DeleteVariable(name, true))
                {
                    LoadEnvironmentVariables();
                    MessageBox.Show("系统环境变量已成功删除！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("删除系统环境变量失败，用户取消了提权或权限不足。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadEnvironmentVariables();
        }

        [RelayCommand]
        private void EditSelected()
        {
            if (SelectedUserVariable != null)
            {
                EditVariable(SelectedUserVariable, false);
            }
            else if (SelectedSystemVariable != null)
            {
                EditVariable(SelectedSystemVariable, true);
            }
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            if (SelectedUserVariable != null)
            {
                DeleteUserVariable();
            }
            else if (SelectedSystemVariable != null)
            {
                DeleteSystemVariable();
            }
        }

        [RelayCommand]
        private void AddUserVariable()
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
                    MessageBox.Show("用户环境变量已添加！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("添加用户环境变量失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void AddSystemVariable()
        {
            var newVariable = new SystemEnvironmentVariable("新变量", "", true);
            var editWindow = new Views.Dialogs.EditEnvironmentVariableWindow();
            editWindow.Owner = Application.Current.MainWindow;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var viewModel = new ViewModels.Dialogs.EditEnvironmentVariableViewModel(newVariable, true, true); // true表示新建变量
            editWindow.DataContext = viewModel;

            if (editWindow.ShowDialog() == true)
            {
                SaveSystemVariableWithUac(viewModel.VariableName, viewModel.VariableValue);
            }
        }

        [RelayCommand]
        private void EditUserVariable()
        {
            if (SelectedUserVariable == null) return;
            EditVariable(SelectedUserVariable, false);
        }

        [RelayCommand]
        private void EditSystemVariable()
        {
            if (SelectedSystemVariable == null) return;
            EditVariable(SelectedSystemVariable, true);
        }

        private void EditVariable(SystemEnvironmentVariable variable, bool isSystemVariable)
        {
            try
            {
                // Create a new window instance each time
                var editWindow = new Views.Dialogs.EditEnvironmentVariableWindow();
                editWindow.Owner = Application.Current.MainWindow;
                editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var viewModel = new ViewModels.Dialogs.EditEnvironmentVariableViewModel(variable, isSystemVariable);
                editWindow.DataContext = viewModel;

                if (editWindow.ShowDialog() == true)
                {
                    // 保存更改 - 根据是否需要管理员权限采用不同策略
                    SaveEnvironmentVariable(viewModel.VariableName, viewModel.VariableValue, isSystemVariable);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"编辑环境变量时出错: {ex.Message}");
            }
        }

        private void SaveEnvironmentVariable(string name, string value, bool isSystemVariable)
        {
            if (isSystemVariable)
            {
                // 系统环境变量需要管理员权限
                SaveSystemVariableWithUac(name, value);
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
                    MessageBox.Show("保存用户环境变量失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveSystemVariableWithUac(string name, string value)
        {
            if (_envService.HasAdminPrivileges())
            {
                // 当前已有管理员权限，直接保存
                if (_envService.SetSystemVariable(name, value))
                {
                    LoadEnvironmentVariables();
                    MessageBox.Show("系统环境变量已成功更新！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("保存系统环境变量失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // 没有管理员权限，直接使用UAC提权（不显示确认对话框）
                if (_envService.SetSystemVariableWithUac(name, value))
                {
                    LoadEnvironmentVariables();
                    MessageBox.Show("系统环境变量已成功更新！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("设置系统环境变量失败，用户取消了提权或权限不足。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}