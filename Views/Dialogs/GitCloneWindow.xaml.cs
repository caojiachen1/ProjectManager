using System.Windows;
using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class GitCloneWindow : FluentWindow
    {
        public GitCloneDialogViewModel ViewModel { get; }

        public GitCloneWindow(GitCloneDialogViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
            
            // 设置暗黑主题
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                Wpf.Ui.Appearance.ApplicationTheme.Dark,
                Wpf.Ui.Controls.WindowBackdropType.Mica,
                true
            );
            
            // 订阅克隆完成事件
            viewModel.CloneCompleted += OnCloneCompleted;
        }

        private void OnCloneCompleted(object? sender, bool success)
        {
            if (success)
            {
                DialogResult = true;
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (ViewModel != null)
            {
                ViewModel.CloneCompleted -= OnCloneCompleted;
            }
            base.OnClosed(e);
        }
    }
}