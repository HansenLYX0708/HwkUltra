using HWKUltra.AI.Abstractions;

namespace HWKUltra.AI.Services.Online
{
    /// <summary>
    /// Azure OpenAI service.
    /// Endpoint format: {endpoint}/openai/deployments/{deployment}/chat/completions?api-version={version}
    /// Uses api-key header instead of Authorization Bearer.
    /// </summary>
    public class AzureOpenAIService : OpenAICompatibleServiceBase
    {
        private readonly string _endpoint;
        private readonly string _deploymentName;
        private readonly string _apiVersion;

        public override LLMProviderType ProviderType => LLMProviderType.AzureOpenAI;
        protected override string ChatCompletionsUrl =>
            $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
        protected override string TestConnectionUrl =>
            $"{_endpoint}/openai/deployments?api-version={_apiVersion}";
        protected override string DefaultModel => _deploymentName;

        // Deployment encodes the model in Azure; don't send "model" in payload.
        protected override bool IncludeModelInPayload => false;

        public AzureOpenAIService(string endpoint, string apiKey, string deploymentName, string apiVersion = "2024-02-15-preview", HttpClient? httpClient = null)
            : base(httpClient)
        {
            _endpoint = endpoint.TrimEnd('/');
            _deploymentName = deploymentName;
            _apiVersion = apiVersion;

            if (!HttpClient.DefaultRequestHeaders.Contains("api-key"))
                HttpClient.DefaultRequestHeaders.Add("api-key", apiKey);
        }
    }
}
