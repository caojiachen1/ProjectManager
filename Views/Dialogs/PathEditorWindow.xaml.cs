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
    // 行号转换器
    public class RowIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return (index + 1).ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
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

        private void PathDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;

            switch (e.Key)
            {
                case Key.Delete:
                    // Delete键删除选中项目
                    if (_viewModel.SelectedPathItem != null)
                    {
                        _viewModel.DeleteCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.Enter:
                    // Enter键编辑选中项目
                    if (_viewModel.SelectedPathItem != null)
                    {
                        _viewModel.EditCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.Insert:
                    // Insert键新建项目
                    _viewModel.NewCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F2:
                    // F2键重命名（编辑）
                    if (_viewModel.SelectedPathItem != null)
                    {
                        _viewModel.EditCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.Up:
                    // Alt+Up 上移
                    if (Keyboard.Modifiers == ModifierKeys.Alt && _viewModel.CanMoveUp)
                    {
                        _viewModel.MoveUpCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Down:
                    // Alt+Down 下移
                    if (Keyboard.Modifiers == ModifierKeys.Alt && _viewModel.CanMoveDown)
                    {
                        _viewModel.MoveDownCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
            }
        }
    }
}