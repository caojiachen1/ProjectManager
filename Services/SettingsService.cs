using System.Text.Json;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings? _cachedSettings;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ProjectManager");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "settings.json");
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _cachedSettings = new AppSettings
                    {
                        DefaultProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Projects")
                    };
                }
            }
            catch
            {
                _cachedSettings = new AppSettings();
            }

            return _cachedSettings;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                await File.WriteAllTextAsync(_settingsFilePath, json);
                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetGitUserNameAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.GitUserName;
        }

        public async Task<string> GetGitUserEmailAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.GitUserEmail;
        }

        public async Task<string> GetGitExecutablePathAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.GitExecutablePath;
        }

        public async Task SetGitUserNameAsync(string userName)
        {
            var settings = await GetSettingsAsync();
            settings.GitUserName = userName;
            await SaveSettingsAsync(settings);
        }

        public async Task SetGitUserEmailAsync(string userEmail)
        {
            var settings = await GetSettingsAsync();
            settings.GitUserEmail = userEmail;
            await SaveSettingsAsync(settings);
        }

        public async Task SetGitExecutablePathAsync(string gitPath)
        {
            var settings = await GetSettingsAsync();
            settings.GitExecutablePath = gitPath;
            await SaveSettingsAsync(settings);
        }

        public async Task<string> GetDefaultProjectPathAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.DefaultProjectPath;
        }

        public async Task SetDefaultProjectPathAsync(string path)
        {
            var settings = await GetSettingsAsync();
            settings.DefaultProjectPath = path;
            await SaveSettingsAsync(settings);
        }

        public async Task<bool> GetAutoStartProjectsAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.AutoStartProjects;
        }

        public async Task SetAutoStartProjectsAsync(bool autoStart)
        {
            var settings = await GetSettingsAsync();
            settings.AutoStartProjects = autoStart;
            await SaveSettingsAsync(settings);
        }
    }
}