using System.Windows;
using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;
using System.ComponentModel;

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
            
            // 监听日志更新，自动滚动到底部
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GitCloneDialogViewModel.CloneLog))
            {
                // 在下一个UI循环中滚动到底部
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogScrollViewer.ScrollToEnd();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
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
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnClosed(e);
        }
    }
}