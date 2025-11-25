using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using ProjectManager.Models;
using System.Windows.Media;

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

        private void UserVariableListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("UserVariableListView_MouseDoubleClick invoked");
            // Ensure the item under the pointer is selected (handles collection refresh/new instances)
            var source = e.OriginalSource as DependencyObject;
            var fe = FindElementWithDataContext<SystemEnvironmentVariable>(source);
            if (fe != null && fe.DataContext is SystemEnvironmentVariable variable)
            {
                ViewModel.SelectedUserVariable = variable;
                if (ViewModel.EditUserVariableCommand.CanExecute(null))
                    ViewModel.EditUserVariableCommand.Execute(null);
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
                if (ViewModel.EditSystemVariableCommand.CanExecute(null))
                    ViewModel.EditSystemVariableCommand.Execute(null);
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
                    if (ViewModel.EditSystemVariableCommand.CanExecute(null))
                        ViewModel.EditSystemVariableCommand.Execute(null);
                }
                else
                {
                    ViewModel.SelectedUserVariable = variable;
                    if (ViewModel.EditUserVariableCommand.CanExecute(null))
                        ViewModel.EditUserVariableCommand.Execute(null);
                }

                e.Handled = true;
            }
        }
    }
}