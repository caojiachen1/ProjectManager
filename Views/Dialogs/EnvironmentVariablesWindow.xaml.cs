using System.Windows;
using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class EnvironmentVariablesWindow : FluentWindow
    {
        public EnvironmentVariablesDialogViewModel ViewModel { get; }

        public EnvironmentVariablesWindow(EnvironmentVariablesDialogViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveChanges();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}