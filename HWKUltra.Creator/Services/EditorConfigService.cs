using System.IO;
using System.Text.Json;
using HWKUltra.Creator.Models;

namespace HWKUltra.Creator.Services
{
    /// <summary>
    /// Loads and saves editor configuration from JSON
    /// </summary>
    public class EditorConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ConfigJson", "FlowEditor", "EditorConfig.json");

        private EditorConfig? _config;

        public EditorConfig GetConfig()
        {
            if (_config != null)
                return _config;

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    _config = JsonSerializer.Deserialize<EditorConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch
            {
                // Fall back to defaults
            }

            _config ??= new EditorConfig();
            return _config;
        }

        public void SaveConfig()
        {
            if (_config == null) return;

            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Silently fail for config save
            }
        }
    }
}
