using System.Globalization;
using System.Windows;

namespace ProjectManager.Services
{
    /// <summary>
    /// 语言服务接口
    /// </summary>
    public interface ILanguageService
    {
        /// <summary>
        /// 当前语言
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// 支持的语言列表
        /// </summary>
        IReadOnlyList<LanguageInfo> SupportedLanguages { get; }

        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="languageCode">语言代码，如 zh-CN, en-US</param>
        void ChangeLanguage(string languageCode);

        /// <summary>
        /// 获取翻译字符串
        /// </summary>
        /// <param name="key">资源键</param>
        /// <returns>翻译后的字符串</returns>
        string GetString(string key);

        /// <summary>
        /// 语言变更事件
        /// </summary>
        event EventHandler<string>? LanguageChanged;
    }

    /// <summary>
    /// 语言信息
    /// </summary>
    public class LanguageInfo
    {
        public string Code { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string NativeName { get; init; } = string.Empty;
    }

    /// <summary>
    /// 语言服务实现
    /// </summary>
    public class LanguageService : ILanguageService
    {
        private readonly ISettingsService _settingsService;
        private string _currentLanguage = "zh-CN";
        private ResourceDictionary? _currentDictionary;

        public event EventHandler<string>? LanguageChanged;

        public string CurrentLanguage => _currentLanguage;

        public IReadOnlyList<LanguageInfo> SupportedLanguages { get; } = new List<LanguageInfo>
        {
            new LanguageInfo { Code = "zh-CN", DisplayName = "Chinese (Simplified)", NativeName = "简体中文" },
            new LanguageInfo { Code = "en-US", DisplayName = "English (US)", NativeName = "English" }
        };

        public LanguageService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// 初始化语言服务，从设置中加载语言
        /// </summary>
        public async Task InitializeAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();
            var savedLanguage = settings.Language;

            if (string.IsNullOrEmpty(savedLanguage))
            {
                // 如果没有保存的语言设置，尝试使用系统语言
                var systemCulture = CultureInfo.CurrentUICulture.Name;
                savedLanguage = SupportedLanguages.Any(l => l.Code == systemCulture) 
                    ? systemCulture 
                    : "zh-CN"; // 默认中文
            }

            ChangeLanguage(savedLanguage);
        }

        public void ChangeLanguage(string languageCode)
        {
            if (!SupportedLanguages.Any(l => l.Code == languageCode))
            {
                languageCode = "zh-CN"; // 回退到默认语言
            }

            if (_currentLanguage == languageCode && _currentDictionary != null)
            {
                return; // 语言未改变
            }

            try
            {
                // 移除旧的语言资源字典
                if (_currentDictionary != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_currentDictionary);
                }

                // 加载新的语言资源字典
                var uri = new Uri($"pack://application:,,,/Resources/Languages/Strings.{languageCode}.xaml", UriKind.Absolute);
                _currentDictionary = new ResourceDictionary { Source = uri };
                
                // 添加到应用程序资源
                Application.Current.Resources.MergedDictionaries.Add(_currentDictionary);

                _currentLanguage = languageCode;

                // 保存语言设置
                Task.Run(async () =>
                {
                    var settings = await _settingsService.GetSettingsAsync();
                    settings.Language = languageCode;
                    await _settingsService.SaveSettingsAsync(settings);
                });

                // 触发语言变更事件
                LanguageChanged?.Invoke(this, languageCode);

                System.Diagnostics.Debug.WriteLine($"Language changed to: {languageCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to change language: {ex.Message}");
            }
        }

        public string GetString(string key)
        {
            try
            {
                if (Application.Current.TryFindResource(key) is string value)
                {
                    return value;
                }
            }
            catch
            {
                // 忽略异常
            }

            return key; // 如果找不到翻译，返回键本身
        }
    }
}
