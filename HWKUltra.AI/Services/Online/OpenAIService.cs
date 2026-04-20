using HWKUltra.AI.Abstractions;

namespace HWKUltra.AI.Services.Online
{
    /// <summary>
    /// OpenAI official API service (https://api.openai.com/v1/chat/completions).
    /// </summary>
    public class OpenAIService : OpenAICompatibleServiceBase
    {
        private readonly string _apiKey;
        private readonly string _defaultModel;
        private readonly string _baseUrl;

        public override LLMProviderType ProviderType => LLMProviderType.OpenAI;
        protected override string ChatCompletionsUrl => $"{_baseUrl}/v1/chat/completions";
        protected override string TestConnectionUrl => $"{_baseUrl}/v1/models";
        protected override string DefaultModel => _defaultModel;

        public OpenAIService(string apiKey, string defaultModel = "gpt-4o-mini", string baseUrl = "https://api.openai.com", HttpClient? httpClient = null)
            : base(httpClient)
        {
            _apiKey = apiKey;
            _defaultModel = defaultModel;
            _baseUrl = baseUrl.TrimEnd('/');

            if (!HttpClient.DefaultRequestHeaders.Contains("Authorization"))
                HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }
}
