using System.Text.Json.Serialization;
using HWKUltra.AI.Abstractions;

namespace HWKUltra.AI.Config
{
    public class LLMConfig
    {
        [JsonPropertyName("defaultProvider")]
        public LLMProviderType DefaultProvider { get; set; } = LLMProviderType.LocalOllama;

        [JsonPropertyName("fallbackToOnline")]
        public bool FallbackToOnline { get; set; } = true;

        [JsonPropertyName("localOllama")]
        public LocalProviderConfig? LocalOllama { get; set; }

        [JsonPropertyName("localLMStudio")]
        public LocalProviderConfig? LocalLMStudio { get; set; }

        [JsonPropertyName("openAI")]
        public OnlineProviderConfig? OpenAI { get; set; }

        [JsonPropertyName("azureOpenAI")]
        public AzureProviderConfig? AzureOpenAI { get; set; }
    }

    public class LocalProviderConfig
    {
        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; } = string.Empty;

        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public class OnlineProviderConfig
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;
    }

    public class AzureProviderConfig
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("deploymentName")]
        public string DeploymentName { get; set; } = string.Empty;

        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = "2024-02-15-preview";

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = new[] { typeof(JsonStringEnumConverter<LLMProviderType>) })]
    [JsonSerializable(typeof(LLMConfig))]
    public partial class LLMConfigJsonContext : JsonSerializerContext
    {
    }
}
