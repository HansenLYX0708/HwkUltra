using System.Text.Json;

namespace HWKUltra.AI.Config
{
    public static class LLMConfigLoader
    {
        /// <summary>
        /// Load configuration from a JSON file. Returns a default config if the file is missing or invalid.
        /// </summary>
        public static LLMConfig Load(string filePath)
        {
            if (!File.Exists(filePath))
                return CreateDefault();

            try
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize(json, LLMConfigJsonContext.Default.LLMConfig);
                return config ?? CreateDefault();
            }
            catch
            {
                return CreateDefault();
            }
        }

        /// <summary>
        /// Save configuration to a JSON file.
        /// </summary>
        public static void Save(LLMConfig config, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, LLMConfigJsonContext.Default.LLMConfig);
            File.WriteAllText(filePath, json);
        }

        private static LLMConfig CreateDefault()
        {
            return new LLMConfig
            {
                DefaultProvider = Abstractions.LLMProviderType.LocalOllama,
                FallbackToOnline = false,
                LocalOllama = new LocalProviderConfig
                {
                    BaseUrl = "http://localhost:11434",
                    DefaultModel = "llama3:8b",
                    Enabled = true
                },
                LocalLMStudio = new LocalProviderConfig
                {
                    BaseUrl = "http://localhost:1234",
                    DefaultModel = "local-model",
                    Enabled = false
                },
                OpenAI = new OnlineProviderConfig
                {
                    ApiKey = "",
                    DefaultModel = "gpt-4o-mini",
                    Enabled = false
                },
                AzureOpenAI = new AzureProviderConfig
                {
                    Endpoint = "",
                    ApiKey = "",
                    DeploymentName = "",
                    Enabled = false
                }
            };
        }
    }
}
