using System;
using System.Windows;
using Microsoft.Win32;
using ProjectManager.ViewModels.Dialogs;

namespace ProjectManager.Views.Dialogs
{
    public partial class PathItemEditWindow
    {
        public PathItemEditWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            
            if (DataContext is PathItemEditViewModel viewModel)
            {
                viewModel.CloseRequested += OnViewModelCloseRequested;
            }
        }

        private void OnViewModelCloseRequested(object? sender, bool result)
        {
            System.Diagnostics.Debug.WriteLine($"PathItemEditWindow.CloseRequested event fired with result: {result}");
            try
            {
                DialogResult = result;
                System.Diagnostics.Debug.WriteLine($"DialogResult set to: {DialogResult}");
                Close();
                System.Diagnostics.Debug.WriteLine("Close() called successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CloseRequested handler: {ex.Message}");
                // 如果设置DialogResult失败，尝试直接关闭
                try
                {
                    Close();
                }
                catch (Exception closeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to close window: {closeEx.Message}");
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is PathItemEditViewModel viewModel)
            {
                viewModel.CloseRequested -= OnViewModelCloseRequested;
            }
            base.OnClosed(e);
        }


    }
}