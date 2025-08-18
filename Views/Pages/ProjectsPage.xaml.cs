using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows.Controls;
using System.Windows;
using Wpf.Ui.Controls;
using ProjectManager.Controls;
using System.Windows.Media;

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

        private void ProjectsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
            if (mainWindow == null) return;

            var host = FindChildByName<Grid>(mainWindow, "ProjectsToolbarHost");
            if (host == null) return;

            // 如果还没有工具栏则创建
            if (host.Children.OfType<ProjectsHeaderToolbar>().FirstOrDefault() is not ProjectsHeaderToolbar toolbar)
            {
                toolbar = new ProjectsHeaderToolbar();
                toolbar.DataContext = this.DataContext;
                host.Children.Add(toolbar);
            }
            else
            {
                toolbar.DataContext = this.DataContext;
            }

            host.Visibility = Visibility.Visible;
        }

        private void ProjectsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
            if (mainWindow == null) return;

            var host = FindChildByName<Grid>(mainWindow, "ProjectsToolbarHost");
            if (host == null) return;

            host.Visibility = Visibility.Collapsed;
        }

        private T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T fe && fe.Name == name)
                    return fe;
                var result = FindChildByName<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
