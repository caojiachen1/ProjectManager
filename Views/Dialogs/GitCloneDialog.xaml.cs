using ProjectManager.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Dialogs
{
    public partial class GitCloneDialog : ContentDialog
    {
        public GitCloneDialogViewModel ViewModel { get; }

        public GitCloneDialog(GitCloneDialogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            
            InitializeComponent();
            
            // 监听属性变化来控制按钮状态
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.CanClone))
                {
                    IsPrimaryButtonEnabled = ViewModel.CanClone;
                }
                if (e.PropertyName == nameof(ViewModel.IsCloning))
                {
                    IsPrimaryButtonEnabled = !ViewModel.IsCloning;
                    IsSecondaryButtonEnabled = !ViewModel.IsCloning;
                }
            };
        }

        protected override async void OnButtonClick(ContentDialogButton button)
        {
            if (button == ContentDialogButton.Primary)
            {
                var result = await ViewModel.CloneRepositoryAsync();
                if (result)
                {
                    base.OnButtonClick(button);
                }
                // 如果克隆失败，不关闭对话框
            }
            else
            {
                base.OnButtonClick(button);
            }
        }
    }
}