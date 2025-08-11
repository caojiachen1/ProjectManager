using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows.Controls;
using System.Windows;
using Wpf.Ui.Controls;

namespace ProjectManager.Views.Pages
{
    public partial class ProjectsPage : Page, INavigableView<ProjectsViewModel>
    {
        public ProjectsViewModel ViewModel { get; }

        public ProjectsPage(ProjectsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.DataContext = this.DataContext; // 设置 ContextMenu 的 DataContext
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
