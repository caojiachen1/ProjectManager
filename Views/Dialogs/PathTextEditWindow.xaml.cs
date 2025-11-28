using System;
using System.Windows;
using ProjectManager.ViewModels.Dialogs;

namespace ProjectManager.Views.Dialogs
{
    public partial class PathTextEditWindow
    {
        private PathTextEditViewModel _viewModel;

        public PathTextEditWindow(PathTextEditViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            _viewModel.CloseRequested += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}