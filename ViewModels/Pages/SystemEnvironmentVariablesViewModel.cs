using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProjectManager.Models;
using ProjectManager.Views.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.ViewModels.Pages
{
    public partial class SystemEnvironmentVariablesViewModel : ObservableObject
    {

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
        private ICollectionView? _filteredUserVariables;

        [ObservableProperty]
        private ICollectionView? _filteredSystemVariables;

        public bool HasUserVariables => UserVariables.Count > 0;
        public bool HasSystemVariables => SystemVariables.Count > 0;
        public bool HasSelectedUserVariable => SelectedUserVariable != null;
        public bool HasSelectedSystemVariable => SelectedSystemVariable != null;

        public SystemEnvironmentVariablesViewModel()
        {
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

        partial void OnSelectedUserVariableChanged(SystemEnvironmentVariable? value)
        {
            OnPropertyChanged(nameof(HasSelectedUserVariable));
        }

        partial void OnSelectedSystemVariableChanged(SystemEnvironmentVariable? value)
        {
            OnPropertyChanged(nameof(HasSelectedSystemVariable));
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadEnvironmentVariables();
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
                    // 保存更改
                    var target = isSystemVariable ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
                    Environment.SetEnvironmentVariable(viewModel.VariableName, viewModel.VariableValue, target);
                    
                    // 重新加载
                    LoadEnvironmentVariables();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"编辑环境变量时出错: {ex.Message}");
            }
        }
    }
}