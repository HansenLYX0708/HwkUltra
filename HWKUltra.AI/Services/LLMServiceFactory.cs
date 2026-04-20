using HWKUltra.AI.Abstractions;
using HWKUltra.AI.Config;
using HWKUltra.AI.Services.Local;
using HWKUltra.AI.Services.Online;

namespace HWKUltra.AI.Services
{
    /// <summary>
    /// Creates and caches ILLMService instances based on LLMConfig.
    /// Supports fallback from local to online when a local provider is unavailable.
    /// </summary>
    public class LLMServiceFactory
    {
        private readonly LLMConfig _config;
        private readonly Dictionary<LLMProviderType, ILLMService> _services = new();

        public LLMConfig Config => _config;

        public LLMServiceFactory(LLMConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            InitializeServices();
        }

        private void InitializeServices()
        {
            if (_config.LocalOllama?.Enabled == true)
            {
                _services[LLMProviderType.LocalOllama] = new OllamaService(
                    _config.LocalOllama.BaseUrl,
                    _config.LocalOllama.DefaultModel);
            }

            if (_config.LocalLMStudio?.Enabled == true)
            {
                _services[LLMProviderType.LocalLMStudio] = new LMStudioService(
                    _config.LocalLMStudio.BaseUrl,
                    _config.LocalLMStudio.DefaultModel);
            }

            if (_config.OpenAI?.Enabled == true && !string.IsNullOrWhiteSpace(_config.OpenAI.ApiKey))
            {
                _services[LLMProviderType.OpenAI] = new OpenAIService(
                    _config.OpenAI.ApiKey,
                    _config.OpenAI.DefaultModel);
            }

            if (_config.AzureOpenAI?.Enabled == true &&
                !string.IsNullOrWhiteSpace(_config.AzureOpenAI.Endpoint) &&
                !string.IsNullOrWhiteSpace(_config.AzureOpenAI.ApiKey) &&
                !string.IsNullOrWhiteSpace(_config.AzureOpenAI.DeploymentName))
            {
                _services[LLMProviderType.AzureOpenAI] = new AzureOpenAIService(
                    _config.AzureOpenAI.Endpoint,
                    _config.AzureOpenAI.ApiKey,
                    _config.AzureOpenAI.DeploymentName,
                    _config.AzureOpenAI.ApiVersion);
            }
        }

        /// <summary>
        /// Get service for the configured default provider.
        /// Throws if no provider is configured.
        /// </summary>
        public ILLMService GetDefaultService() => GetService(_config.DefaultProvider);

        /// <summary>
        /// Get service by provider type. Applies fallback-to-online rule if requested provider
        /// is local and unavailable, and FallbackToOnline is enabled.
        /// </summary>
        public ILLMService GetService(LLMProviderType provider)
        {
            if (_services.TryGetValue(provider, out var service))
                return service;

            if (_config.FallbackToOnline && IsLocalProvider(provider))
            {
                if (_services.TryGetValue(LLMProviderType.OpenAI, out var openai)) return openai;
                if (_services.TryGetValue(LLMProviderType.AzureOpenAI, out var azure)) return azure;
            }

            if (_services.Count > 0)
                return _services.Values.First();

            throw new InvalidOperationException(
                $"No LLM service available. Provider '{provider}' is not configured and no fallback exists.");
        }

        /// <summary>Try to get a service; returns false if not configured.</summary>
        public bool TryGetService(LLMProviderType provider, out ILLMService? service)
        {
            return _services.TryGetValue(provider, out service);
        }

        public IEnumerable<LLMProviderType> AvailableProviders => _services.Keys;

        public IEnumerable<ILLMService> AllServices => _services.Values;

        /// <summary>
        /// Test connectivity for all configured providers. Returns a map of provider->available.
        /// </summary>
        public async Task<Dictionary<LLMProviderType, bool>> TestAllConnectionsAsync(CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<LLMProviderType, bool>();
            foreach (var kvp in _services)
            {
                try
                {
                    results[kvp.Key] = await kvp.Value.TestConnectionAsync(cancellationToken);
                }
                catch
                {
                    results[kvp.Key] = false;
                }
            }
            return results;
        }

        private static bool IsLocalProvider(LLMProviderType provider) =>
            provider == LLMProviderType.LocalOllama || provider == LLMProviderType.LocalLMStudio;
    }
}
