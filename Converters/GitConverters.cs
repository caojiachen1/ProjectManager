using System.Globalization;
using System.Windows.Data;
using ProjectManager.Models;
using Wpf.Ui.Controls;

namespace ProjectManager.Converters
{
    public class GitStatusToBadgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GitStatus status)
            {
                return status switch
                {
                    GitStatus.Clean => ControlAppearance.Success,
                    GitStatus.Modified => ControlAppearance.Caution,
                    GitStatus.Staged => ControlAppearance.Info,
                    GitStatus.Conflicted => ControlAppearance.Danger,
                    GitStatus.Untracked => ControlAppearance.Secondary,
                    _ => ControlAppearance.Secondary
                };
            }
            return ControlAppearance.Secondary;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
