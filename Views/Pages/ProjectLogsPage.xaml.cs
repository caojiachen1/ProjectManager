using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows.Controls;

namespace ProjectManager.Views.Pages
{
    public partial class ProjectLogsPage : Page, INavigableView<ProjectLogsViewModel>
    {
        public ProjectLogsViewModel ViewModel { get; }

        public ProjectLogsPage(ProjectLogsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            
            // 监听日志更新以自动滚动
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.LogContent) && ViewModel.AutoScroll)
                {
                    Dispatcher.BeginInvoke(() => LogScrollViewer.ScrollToEnd());
                }
            };
        }
    }
}
