using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using ProjectManager.Models;
using System.Windows.Media;
using ProjectManager.Controls;
using System.Linq;

namespace ProjectManager.Views.Pages
{
    public partial class SystemEnvironmentVariablesPage : INavigableView<SystemEnvironmentVariablesViewModel>
    {
        public SystemEnvironmentVariablesViewModel ViewModel { get; }

        public SystemEnvironmentVariablesPage(SystemEnvironmentVariablesViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();
            // Listen for tunneling mouse-down and detect double-clicks (use ClickCount) to robustly catch double-clicks
            AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnAnyPreviewMouseLeftButtonDown), true);
        }

        private void SystemEnvironmentVariablesPage_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
            if (mainWindow == null) return;

            // 隐藏其他工具栏
            var projectsHost = FindChildByName<Grid>(mainWindow, "ProjectsToolbarHost");
            var performanceHost = FindChildByName<Grid>(mainWindow, "PerformanceToolbarHost");
            if (projectsHost != null)
            {
                projectsHost.Visibility = Visibility.Collapsed;
            }
            if (performanceHost != null)
            {
                performanceHost.Visibility = Visibility.Collapsed;
            }

            // 显示环境变量工具栏
            var envHost = FindChildByName<Grid>(mainWindow, "EnvironmentVariablesToolbarHost");
            if (envHost == null) return;

            // 如果还没有工具栏则创建
            if (envHost.Children.OfType<EnvironmentVariablesHeaderToolbar>().FirstOrDefault() is not EnvironmentVariablesHeaderToolbar toolbar)
            {
                toolbar = new EnvironmentVariablesHeaderToolbar();
                toolbar.DataContext = this.DataContext;
                envHost.Children.Add(toolbar);
            }
            else
            {
                toolbar.DataContext = this.DataContext;
            }

            envHost.Visibility = Visibility.Visible;
        }

        private void SystemEnvironmentVariablesPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
            if (mainWindow == null) return;

            var envHost = FindChildByName<Grid>(mainWindow, "EnvironmentVariablesToolbarHost");
            if (envHost == null) return;

            envHost.Visibility = Visibility.Collapsed;
        }

        private void UserVariableListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("UserVariableListView_MouseDoubleClick invoked");
            // Ensure the item under the pointer is selected (handles collection refresh/new instances)
            var source = e.OriginalSource as DependencyObject;
            var fe = FindElementWithDataContext<SystemEnvironmentVariable>(source);
            if (fe != null && fe.DataContext is SystemEnvironmentVariable variable)
            {
                ViewModel.SelectedUserVariable = variable;
                ViewModel.SelectedSystemVariable = null;
                if (ViewModel.EditSelectedCommand.CanExecute(null))
                    ViewModel.EditSelectedCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void SystemVariableListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("SystemVariableListView_MouseDoubleClick invoked");
            var source = e.OriginalSource as DependencyObject;
            var fe = FindElementWithDataContext<SystemEnvironmentVariable>(source);
            if (fe != null && fe.DataContext is SystemEnvironmentVariable variable)
            {
                ViewModel.SelectedSystemVariable = variable;
                ViewModel.SelectedUserVariable = null;
                if (ViewModel.EditSelectedCommand.CanExecute(null))
                    ViewModel.EditSelectedCommand.Execute(null);
                e.Handled = true;
            }
        }

        private static FrameworkElement? FindElementWithDataContext<TData>(DependencyObject? current)
        {
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is TData)
                    return fe;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void OnAnyPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only handle actual double-clicks
            if (e.ClickCount != 2) return;
            Debug.WriteLine("OnAnyPreviewMouseLeftButtonDown double-click detected");

            var source = e.OriginalSource as DependencyObject;
            var fe = FindElementWithDataContext<SystemEnvironmentVariable>(source);
            if (fe != null && fe.DataContext is SystemEnvironmentVariable variable)
            {
                // synchronize selection and open editor based on variable type
                if (variable.IsSystemVariable)
                {
                    ViewModel.SelectedSystemVariable = variable;
                    ViewModel.SelectedUserVariable = null;
                }
                else
                {
                    ViewModel.SelectedUserVariable = variable;
                    ViewModel.SelectedSystemVariable = null;
                }

                // 使用统一的编辑命令
                if (ViewModel.EditSelectedCommand.CanExecute(null))
                    ViewModel.EditSelectedCommand.Execute(null);

                e.Handled = true;
            }
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