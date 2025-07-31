using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class GitManagementDialog : ContentDialog
    {
        public GitManagementDialogViewModel? ViewModel { get; private set; }

        public GitManagementDialog()
        {
            InitializeComponent();
        }

        public GitManagementDialog(GitManagementDialogViewModel viewModel) : this()
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        public void SetViewModel(GitManagementDialogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
        }
    }
}
