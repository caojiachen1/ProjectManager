using System.Windows;
using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class GitManagementWindow : FluentWindow
    {
        public GitManagementDialogViewModel? ViewModel { get; private set; }

        public GitManagementWindow()
        {
            InitializeComponent();
        }

        public GitManagementWindow(GitManagementDialogViewModel viewModel) : this()
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        public void SetViewModel(GitManagementDialogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
