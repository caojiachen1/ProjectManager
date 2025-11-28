using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectManager.ViewModels.Dialogs
{
    public partial class PathTextEditViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pathText = string.Empty;

        public event EventHandler<bool>? CloseRequested;

        public PathTextEditViewModel(string pathText)
        {
            _pathText = pathText;
        }

        [RelayCommand]
        private void Save()
        {
            CloseRequested?.Invoke(this, true);
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(this, false);
        }
    }
}