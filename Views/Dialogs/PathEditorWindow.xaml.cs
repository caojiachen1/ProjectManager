using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ProjectManager.ViewModels.Dialogs;

namespace ProjectManager.Views.Dialogs
{
    // 反向布尔值到可见性转换器
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return false;
        }
    }

    public partial class PathEditorWindow
    {
        private PathEditorViewModel _viewModel;

        public PathEditorWindow(PathEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            _viewModel.CloseRequested += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
        }

        private void PathListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 双击时编辑选中的项目
            if (_viewModel?.SelectedPathItem != null)
            {
                _viewModel.EditCommand.Execute(null);
            }
        }
    }
}