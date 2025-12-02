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
using CommunityToolkit.Mvvm.Input;

namespace ProjectManager.Views.Pages
{
    public partial class SystemEnvironmentVariablesPage : INavigableView<SystemEnvironmentVariablesViewModel>, INavigationAware
    {
        public SystemEnvironmentVariablesViewModel ViewModel { get; }
        
        // 缓存视觉树查找结果
        private Grid? _cachedProjectsHost;
        private Grid? _cachedPerformanceHost;
        private Grid? _cachedEnvHost;
        private bool _toolbarCacheInitialized = false;

        public SystemEnvironmentVariablesPage(SystemEnvironmentVariablesViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();
            // Listen for tunneling mouse-down and detect double-clicks (use ClickCount) to robustly catch double-clicks
            AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnAnyPreviewMouseLeftButtonDown), true);
        }
        
        public void OnNavigatedTo()
        {
            // 异步初始化数据
            _ = ViewModel.EnsureInitializedAsync();
        }

        public void OnNavigatedFrom() { }

        public async Task OnNavigatedToAsync()
        {
            await ViewModel.EnsureInitializedAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void SystemEnvironmentVariablesPage_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
            if (mainWindow == null) return;

            // 使用缓存的视觉树查找结果
            if (!_toolbarCacheInitialized)
            {
                _cachedProjectsHost = FindChildByName<Grid>(mainWindow, "ProjectsToolbarHost");
                _cachedPerformanceHost = FindChildByName<Grid>(mainWindow, "PerformanceToolbarHost");
                _cachedEnvHost = FindChildByName<Grid>(mainWindow, "EnvironmentVariablesToolbarHost");
                _toolbarCacheInitialized = true;
            }

            // 隐藏其他工具栏
            if (_cachedProjectsHost != null)
            {
                _cachedProjectsHost.Visibility = Visibility.Collapsed;
            }
            if (_cachedPerformanceHost != null)
            {
                _cachedPerformanceHost.Visibility = Visibility.Collapsed;
            }

            // 显示环境变量工具栏
            if (_cachedEnvHost == null) return;

            // 如果还没有工具栏则创建
            if (_cachedEnvHost.Children.OfType<EnvironmentVariablesHeaderToolbar>().FirstOrDefault() is not EnvironmentVariablesHeaderToolbar toolbar)
            {
                toolbar = new EnvironmentVariablesHeaderToolbar();
                toolbar.DataContext = this.DataContext;
                _cachedEnvHost.Children.Add(toolbar);
            }
            else
            {
                toolbar.DataContext = this.DataContext;
            }

            _cachedEnvHost.Visibility = Visibility.Visible;
        }

        private void SystemEnvironmentVariablesPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_cachedEnvHost != null)
            {
                _cachedEnvHost.Visibility = Visibility.Collapsed;
            }
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
                }
                else
                {
                    ViewModel.SelectedUserVariable = variable;
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

        private void UserVariableDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null) return;

            switch (e.Key)
            {
                case Key.Delete:
                    // Delete键删除选中用户变量
                    if (ViewModel.SelectedUserVariable != null)
                    {
                        ViewModel.DeleteUserVariableCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.Enter:
                case Key.F2:
                    // Enter键或F2键编辑选中用户变量
                    if (ViewModel.SelectedUserVariable != null)
                    {
                        ViewModel.EditUserVariableCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void SystemVariableDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null) return;

            switch (e.Key)
            {
                case Key.Delete:
                    // Delete键删除选中系统变量
                    if (ViewModel.SelectedSystemVariable != null)
                    {
                        ViewModel.DeleteSystemVariableCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.Enter:
                case Key.F2:
                    // Enter键或F2键编辑选中系统变量
                    if (ViewModel.SelectedSystemVariable != null)
                    {
                        ViewModel.EditSystemVariableCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
            }
        }
    }
}