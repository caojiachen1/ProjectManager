using System.Windows;
using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class DotNetProjectSettingsWindow : FluentWindow
    {
        public DotNetProjectSettingsViewModel ViewModel { get; }

        public DotNetProjectSettingsWindow(DotNetProjectSettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();
            
            // 设置事件处理
            ViewModel.ProjectSaved += (s, project) => { DialogResult = true; Close(); };
            ViewModel.ProjectDeleted += (s, projectId) => { DialogResult = true; Close(); };
            ViewModel.DialogCancelled += (s, e) => { DialogResult = false; Close(); };
        }
        
        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }
    }
}
