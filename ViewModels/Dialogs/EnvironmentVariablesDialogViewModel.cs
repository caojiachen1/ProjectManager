using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectManager.Models;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class EnvironmentVariablesDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private Project? _project;

        [ObservableProperty]
        private ObservableCollection<EnvironmentVariable> _environmentVariables = new();

        [ObservableProperty]
        private EnvironmentVariable? _selectedVariable;

        [ObservableProperty]
        private string _newVariableName = string.Empty;

        [ObservableProperty]
        private string _newVariableValue = string.Empty;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ICollectionView? _filteredVariables;

        public EnvironmentVariablesDialogViewModel()
        {
            FilteredVariables = CollectionViewSource.GetDefaultView(EnvironmentVariables);
            FilteredVariables.Filter = FilterVariables;
        }

        public void LoadProject(Project project)
        {
            Project = project;
            EnvironmentVariables.Clear();

            foreach (var kvp in project.EnvironmentVariables)
            {
                EnvironmentVariables.Add(new EnvironmentVariable
                {
                    Name = kvp.Key,
                    Value = kvp.Value,
                    IsEnabled = true
                });
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredVariables?.Refresh();
        }

        private bool FilterVariables(object obj)
        {
            if (obj is not EnvironmentVariable variable) return false;

            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                return variable.Name.ToLower().Contains(searchLower) ||
                       variable.Value.ToLower().Contains(searchLower);
            }

            return true;
        }

        [RelayCommand]
        private void AddVariable()
        {
            if (string.IsNullOrWhiteSpace(NewVariableName))
                return;

            // 检查是否已存在同名变量
            if (EnvironmentVariables.Any(v => v.Name.Equals(NewVariableName, StringComparison.OrdinalIgnoreCase)))
                return;

            var newVar = new EnvironmentVariable
            {
                Name = NewVariableName.Trim(),
                Value = NewVariableValue?.Trim() ?? string.Empty,
                IsEnabled = true
            };

            EnvironmentVariables.Add(newVar);
            
            // 清空输入框
            NewVariableName = string.Empty;
            NewVariableValue = string.Empty;
        }

        [RelayCommand]
        private void RemoveVariable(EnvironmentVariable? variable)
        {
            if (variable != null)
            {
                EnvironmentVariables.Remove(variable);
            }
        }

        [RelayCommand]
        private void RemoveSelectedVariable()
        {
            if (SelectedVariable != null)
            {
                EnvironmentVariables.Remove(SelectedVariable);
                SelectedVariable = null;
            }
        }

        [RelayCommand]
        private void ClearAllVariables()
        {
            EnvironmentVariables.Clear();
        }

        public void SaveChanges()
        {
            if (Project == null) return;

            Project.EnvironmentVariables.Clear();
            foreach (var variable in EnvironmentVariables.Where(v => v.IsEnabled))
            {
                if (!string.IsNullOrWhiteSpace(variable.Name))
                {
                    Project.EnvironmentVariables[variable.Name] = variable.Value ?? string.Empty;
                }
            }
        }
    }
}