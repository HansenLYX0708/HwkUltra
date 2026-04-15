using System.IO;
using System.Text.Json;
using HWKUltra.UI.Models;

namespace HWKUltra.UI.Services
{
    /// <summary>
    /// Manages application-wide settings with JSON persistence
    /// </summary>
    public class AppSettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Local", "configs", "appsettings.json");

        private AppSettings? _settings;

        public AppSettings Settings => _settings ??= Load();

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch
            {
                // Fall back to defaults
            }

            _settings ??= new AppSettings();
            return _settings;
        }

        public void Save()
        {
            if (_settings == null) return;

            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Silently fail for settings save
            }
        }

        /// <summary>
        /// Resolve a path that may be relative to the application base directory
        /// </summary>
        public string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
        }
    }
}
