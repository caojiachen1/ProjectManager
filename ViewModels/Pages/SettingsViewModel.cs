using System.IO;
using System.Collections.ObjectModel;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using ProjectManager.Services;
using ProjectManager.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ProjectManager.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly ISettingsService _settingsService;
        private readonly ILanguageService _languageService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        [ObservableProperty]
        private string _gitUserName = string.Empty;

        [ObservableProperty]
        private string _gitUserEmail = string.Empty;

        [ObservableProperty]
        private string _gitExecutablePath = string.Empty;

        [ObservableProperty]
        private string _defaultProjectPath = string.Empty;

        [ObservableProperty]
        private bool _autoStartProjects = false;

        [ObservableProperty]
        private string _defaultGitBranch = "main";

        [ObservableProperty]
        private bool _autoFetchGitUpdates = true;

        [ObservableProperty]
        private int _projectRefreshInterval = 30;

        [ObservableProperty]
        private bool _showNotifications = true;

        [ObservableProperty]
        private string _preferredTerminal = "PowerShell";

        [ObservableProperty]
        private string _preferredEditor = "VS Code";

        [ObservableProperty]
        private bool _autoSaveProjects = true;

        [ObservableProperty]
        private int _maxRecentProjects = 10;

        [ObservableProperty]
        private bool _useCmdChcp65001 = true;

        [ObservableProperty]
        private string _defaultStartupPage = "Dashboard";

        [ObservableProperty]
        private bool _showTerminalTimestamps = false;

        [ObservableProperty]
        private string _selectedLanguage = "zh-CN";

        [ObservableProperty]
        private ObservableCollection<LanguageInfo> _availableLanguages = new();

        public SettingsViewModel(ISettingsService settingsService, ILanguageService languageService)
        {
            _settingsService = settingsService;
            _languageService = languageService;
            
            // 初始化可用语言列表
            foreach (var lang in _languageService.SupportedLanguages)
            {
                AvailableLanguages.Add(lang);
            }
            
            // 监听属性变化以自动保存设置
            PropertyChanged += OnPropertyChanged;
        }

        private async void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!_isInitialized) return;

            // 处理语言变更 - 实时切换
            if (e.PropertyName == nameof(SelectedLanguage))
            {
                if (!string.IsNullOrEmpty(SelectedLanguage))
                {
                    _languageService.ChangeLanguage(SelectedLanguage);
                }
                return;
            }

            // 当设置属性发生变化时自动保存
            if (e.PropertyName != nameof(AppVersion) && e.PropertyName != nameof(AvailableLanguages))
            {
                await SaveAllSettingsAsync();
            }
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                await InitializeViewModelAsync();
        }

        public async Task OnNavigatedFromAsync()
        {
            await SaveAllSettingsAsync();
        }

        private async Task InitializeViewModelAsync()
        {
            AppVersion = $"项目管理器 - {GetAssemblyVersion()}";

            var settings = await _settingsService.GetSettingsAsync();
            
            // 优先从设置中加载主题，若为 Unknown 则回退到系统当前主题
            CurrentTheme = settings.Theme == ApplicationTheme.Unknown ? ApplicationThemeManager.GetAppTheme() : settings.Theme;
            GitUserName = settings.GitUserName;
            GitUserEmail = settings.GitUserEmail;
            GitExecutablePath = settings.GitExecutablePath;
            DefaultProjectPath = settings.DefaultProjectPath;
            AutoStartProjects = settings.AutoStartProjects;
            DefaultGitBranch = settings.DefaultGitBranch;
            AutoFetchGitUpdates = settings.AutoFetchGitUpdates;
            ProjectRefreshInterval = settings.ProjectRefreshInterval;
            ShowNotifications = settings.ShowNotifications;
            PreferredTerminal = settings.PreferredTerminal;
            AutoSaveProjects = settings.AutoSaveProjects;
            MaxRecentProjects = settings.MaxRecentProjects;
            UseCmdChcp65001 = settings.UseCmdChcp65001;
            DefaultStartupPage = settings.DefaultStartupPage;
            ShowTerminalTimestamps = settings.ShowTerminalTimestamps;
            SelectedLanguage = _languageService.CurrentLanguage;

            _isInitialized = true;
        }

        private async Task SaveAllSettingsAsync()
        {
            var settings = new AppSettings
            {
                GitUserName = GitUserName,
                GitUserEmail = GitUserEmail,
                GitExecutablePath = GitExecutablePath,
                DefaultProjectPath = DefaultProjectPath,
                AutoStartProjects = AutoStartProjects,
                DefaultGitBranch = DefaultGitBranch,
                AutoFetchGitUpdates = AutoFetchGitUpdates,
                ProjectRefreshInterval = ProjectRefreshInterval,
                ShowNotifications = ShowNotifications,
                PreferredTerminal = PreferredTerminal,
                AutoSaveProjects = AutoSaveProjects,
                MaxRecentProjects = MaxRecentProjects,
                UseCmdChcp65001 = UseCmdChcp65001,
                DefaultStartupPage = DefaultStartupPage,
                ShowTerminalTimestamps = ShowTerminalTimestamps,
                Theme = CurrentTheme,
                Language = SelectedLanguage
            };

            await _settingsService.SaveSettingsAsync(settings);
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;
                    break;

                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;
                    break;
            }
        }

        [RelayCommand]
        private void BrowseDefaultProjectPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择默认项目路径",
                InitialDirectory = DefaultProjectPath
            };

            if (dialog.ShowDialog() == true)
            {
                DefaultProjectPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void BrowseGitExecutablePath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择Git可执行文件",
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                InitialDirectory = string.IsNullOrEmpty(GitExecutablePath) ? 
                    @"C:\Program Files\Git\bin" : 
                    Path.GetDirectoryName(GitExecutablePath)
            };

            if (dialog.ShowDialog() == true)
            {
                GitExecutablePath = dialog.FileName;
            }
        }

        [RelayCommand]
        private async Task SaveSettings()
        {
            await SaveAllSettingsAsync();
        }

        [RelayCommand]
        private async Task ResetSettings()
        {
            var settings = new AppSettings
            {
                DefaultProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Projects"),
                DefaultStartupPage = "Dashboard",
                Language = "zh-CN"
            };
            
            await _settingsService.SaveSettingsAsync(settings);
            await InitializeViewModelAsync();
        }

        [RelayCommand]
        private void ChangeLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return;
            
            _languageService.ChangeLanguage(languageCode);
            SelectedLanguage = languageCode;
        }
    }
}
