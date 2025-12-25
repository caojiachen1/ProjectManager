using System.Collections.ObjectModel;
using ProjectManager.Services;
using Wpf.Ui.Controls;

namespace ProjectManager.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILanguageService _languageService;

        [ObservableProperty]
        private string _applicationTitle = "通用项目管理器";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new();

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new();

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "主页", Tag = "tray_home" }
        };

        public MainWindowViewModel(ILanguageService languageService)
        {
            _languageService = languageService;
            
            // 初始化导航项
            UpdateNavigationItems();
            
            // 订阅语言变更事件
            _languageService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, string languageCode)
        {
            // 更新标题
            ApplicationTitle = _languageService.GetString("AppTitle");
            
            // 更新导航项
            UpdateNavigationItems();
        }

        private void UpdateNavigationItems()
        {
            MenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem()
                {
                    Content = _languageService.GetString("Nav_Dashboard"),
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                    TargetPageType = typeof(Views.Pages.DashboardPage)
                },
                new NavigationViewItem()
                {
                    Content = _languageService.GetString("Nav_Projects"),
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Apps24 },
                    TargetPageType = typeof(Views.Pages.ProjectsPage)
                },
                new NavigationViewItem()
                {
                    Content = _languageService.GetString("Nav_Terminal"),
                    Icon = new SymbolIcon { Symbol = SymbolRegular.WindowConsole20 },
                    TargetPageType = typeof(Views.Pages.TerminalPage)
                },
                new NavigationViewItem()
                {
                    Content = _languageService.GetString("Nav_Performance"),
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Pulse24 },
                    TargetPageType = typeof(Views.Pages.PerformancePage)
                },
                new NavigationViewItem()
                {
                    Content = _languageService.GetString("Nav_Environment"),
                    Icon = new SymbolIcon { Symbol = SymbolRegular.BracesVariable24 },
                    TargetPageType = typeof(Views.Pages.SystemEnvironmentVariablesPage)
                }
            };

            FooterMenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem()
                {
                    Content = _languageService.GetString("Nav_Settings"),
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                    TargetPageType = typeof(Views.Pages.SettingsPage)
                }
            };

            TrayMenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem { Header = _languageService.GetString("Tray_Home"), Tag = "tray_home" }
            };
        }
    }
}
