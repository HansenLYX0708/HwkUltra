using HWKUltra.AI.Abstractions;

namespace HWKUltra.AI.Services.Local
{
    /// <summary>
    /// LM Studio local service. Exposes an OpenAI-compatible /v1/chat/completions endpoint.
    /// Default: http://localhost:1234
    /// </summary>
    public class LMStudioService : OpenAICompatibleServiceBase
    {
        private readonly string _baseUrl;
        private readonly string _defaultModel;

        public override LLMProviderType ProviderType => LLMProviderType.LocalLMStudio;
        protected override string ChatCompletionsUrl => $"{_baseUrl}/v1/chat/completions";
        protected override string TestConnectionUrl => $"{_baseUrl}/v1/models";
        protected override string DefaultModel => _defaultModel;

        public LMStudioService(string baseUrl = "http://localhost:1234", string defaultModel = "local-model", HttpClient? httpClient = null)
            : base(httpClient)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _defaultModel = defaultModel;
        }
    }
}
