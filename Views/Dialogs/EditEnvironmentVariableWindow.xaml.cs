using System.Windows;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class EditEnvironmentVariableWindow : FluentWindow
    {
        public EditEnvironmentVariableWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}