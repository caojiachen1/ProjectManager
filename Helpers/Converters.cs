using System.Globalization;
using System.Windows.Data;
using System.Windows;
using ProjectManager.Models;

namespace ProjectManager.Helpers
{
    public class StatusToBadgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status switch
                {
                    ProjectStatus.Running => "Success",
                    ProjectStatus.Stopped => "Secondary", 
                    ProjectStatus.Starting => "Info",
                    ProjectStatus.Stopping => "Caution",
                    ProjectStatus.Error => "Danger",
                    _ => "Secondary"
                };
            }
            return "Secondary";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToStartEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status == ProjectStatus.Stopped || status == ProjectStatus.Error;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToStopEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status == ProjectStatus.Running || status == ProjectStatus.Starting;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ResourceStringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string resourceKey)
            {
                var format = Application.Current.TryFindResource(resourceKey) as string;
                if (!string.IsNullOrEmpty(format))
                {
                    return string.Format(format, value);
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class FrameworkToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string framework && !string.IsNullOrEmpty(framework))
            {
                return framework switch
                {
                    "ComfyUI" => Application.Current.FindResource("FrameworkDesc_ComfyUI") ?? "ComfyUI image generation workflow",
                    "Node.js" => Application.Current.FindResource("FrameworkDesc_NodeJS") ?? "Node.js JavaScript runtime",
                    ".NET" => Application.Current.FindResource("FrameworkDesc_DotNet") ?? ".NET application",
                    "其他" => Application.Current.FindResource("FrameworkDesc_Other") ?? "Custom project type",
                    _ => ""
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToInverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class StatusToToggleButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status switch
                {
                    ProjectStatus.Running => Application.Current.FindResource("Button_Stop") ?? "Stop",
                    ProjectStatus.Starting => Application.Current.FindResource("Status_Starting") ?? "Starting",
                    ProjectStatus.Stopping => Application.Current.FindResource("Status_Stopping") ?? "Stopping",
                    ProjectStatus.Stopped => Application.Current.FindResource("Button_Start") ?? "Start",
                    ProjectStatus.Error => Application.Current.FindResource("Button_Start") ?? "Start",
                    _ => Application.Current.FindResource("Button_Start") ?? "Start"
                };
            }
            return Application.Current.FindResource("Button_Start") ?? "Start";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToToggleButtonIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status switch
                {
                    ProjectStatus.Running => "Stop24",
                    ProjectStatus.Starting => "Play24",
                    ProjectStatus.Stopping => "Stop24", 
                    ProjectStatus.Stopped => "Play24",
                    ProjectStatus.Error => "Play24",
                    _ => "Play24"
                };
            }
            return "Play24";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToToggleButtonEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status != ProjectStatus.Starting && status != ProjectStatus.Stopping;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToToggleButtonAppearanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectStatus status)
            {
                return status switch
                {
                    ProjectStatus.Running => "Danger",      // 红色，表示停止操作
                    ProjectStatus.Starting => "Info",       // 信息色，表示启动中
                    ProjectStatus.Stopping => "Caution",    // 警告色，表示停止中
                    ProjectStatus.Stopped => "Primary",     // 主色，表示启动操作
                    ProjectStatus.Error => "Primary",       // 主色，表示启动操作
                    _ => "Primary"
                };
            }
            return "Primary";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FrameworkToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string framework && parameter is string targetFramework)
            {
                return string.Equals(framework, targetFramework, StringComparison.OrdinalIgnoreCase) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将项目的 Framework 是否为 "ComfyUI" 映射为 Visibility，用于显示 ComfyUI 专用控件。
    /// </summary>
    public class ComfyUIFrameworkToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string framework &&
                !string.IsNullOrWhiteSpace(framework) &&
                framework.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProjectCommandParameterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is Project project && values[1] is string command)
            {
                return (project, command);
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FrameworkToLocalizedNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string framework && !string.IsNullOrEmpty(framework))
            {
                return framework switch
                {
                    "ComfyUI" => Application.Current.FindResource("Framework_ComfyUI") ?? "ComfyUI",
                    "Node.js" => Application.Current.FindResource("Framework_NodeJS") ?? "Node.js",
                    ".NET" => Application.Current.FindResource("Framework_DotNet") ?? ".NET",
                    "其他" => Application.Current.FindResource("Framework_Other") ?? "Other",
                    _ => framework
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
