using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class ProjectEditDialog : ContentDialog
    {
        public ProjectEditDialogViewModel ViewModel { get; }

        public ProjectEditDialog(ProjectEditDialogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();
        }
    }
}
