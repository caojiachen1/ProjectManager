using System.Globalization;
using System.Windows.Data;
using System.Windows;
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

    public class HasGitRepositoryToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Project project)
            {
                // 显示Git按钮的条件：
                // 1. 项目本身是Git仓库（GitInfo.IsGitRepository == true）
                // 2. 或者项目包含Git仓库（GitRepositories.Count > 0）
                var hasGitRepo = (project.GitInfo?.IsGitRepository == true) || 
                                (project.GitRepositories?.Count > 0);
                return hasGitRepo ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
