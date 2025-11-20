using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjectManager.Controls;
using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ProjectManager.Views.Pages
{
    public partial class PerformancePage : Page, INavigableView<PerformanceViewModel>
    {
        public PerformanceViewModel ViewModel { get; }

        public PerformancePage(PerformanceViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void PerformancePage_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
            if (mainWindow == null) return;

            var performanceHost = FindChildByName<Grid>(mainWindow, "PerformanceToolbarHost");
            var projectsHost = FindChildByName<Grid>(mainWindow, "ProjectsToolbarHost");

            if (projectsHost != null)
            {
                projectsHost.Visibility = Visibility.Collapsed;
            }

            if (performanceHost == null) return;

            if (performanceHost.Children.OfType<PerformanceHeaderToolbar>().FirstOrDefault() is not PerformanceHeaderToolbar toolbar)
            {
                toolbar = new PerformanceHeaderToolbar();
                toolbar.DataContext = DataContext;
                performanceHost.Children.Add(toolbar);
            }
            else
            {
                toolbar.DataContext = DataContext;
            }

            performanceHost.Visibility = Visibility.Visible;
        }

        private void PerformancePage_Unloaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
            if (mainWindow == null) return;

            var performanceHost = FindChildByName<Grid>(mainWindow, "PerformanceToolbarHost");
            if (performanceHost != null)
            {
                performanceHost.Visibility = Visibility.Collapsed;
            }
        }

        private T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                    return element;

                var result = FindChildByName<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
