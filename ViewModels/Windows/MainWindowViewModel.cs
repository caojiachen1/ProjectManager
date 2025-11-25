using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace ProjectManager.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "通用项目管理器";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "仪表板",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "项目管理",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Apps24 },
                TargetPageType = typeof(Views.Pages.ProjectsPage)
            },
            new NavigationViewItem()
            {
                Content = "终端控制台",
                Icon = new SymbolIcon { Symbol = SymbolRegular.WindowConsole20 },
                TargetPageType = typeof(Views.Pages.TerminalPage)
            },
            new NavigationViewItem()
            {
                Content = "性能监控",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Pulse24 },
                TargetPageType = typeof(Views.Pages.PerformancePage)
            },
            new NavigationViewItem()
            {
                Content = "系统环境变量",
                Icon = new SymbolIcon { Symbol = SymbolRegular.BracesVariable24 },
                TargetPageType = typeof(Views.Pages.SystemEnvironmentVariablesPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "主页", Tag = "tray_home" }
        };
    }
}
