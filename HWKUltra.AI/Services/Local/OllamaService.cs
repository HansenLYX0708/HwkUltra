using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HWKUltra.AI.Abstractions;
using HWKUltra.AI.Utils;

namespace HWKUltra.AI.Services.Local
{
    /// <summary>
    /// Ollama local LLM service (http://localhost:11434 by default).
    /// Uses native Ollama /api/chat protocol (NDJSON streaming).
    /// </summary>
    public class OllamaService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _defaultModel;

        public LLMProviderType ProviderType => LLMProviderType.LocalOllama;
        public bool IsAvailable { get; private set; } = true;

        public OllamaService(string baseUrl = "http://localhost:11434", string defaultModel = "llama3:8b", HttpClient? httpClient = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _defaultModel = defaultModel;
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        }

        public async Task<LLMCompletionResponse> CompleteAsync(LLMCompletionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = BuildPayload(request, stream: false);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    IsAvailable = false;
                    return new LLMCompletionResponse
                    {
                        Success = false,
                        Error = $"HTTP {(int)response.StatusCode}: {err}"
                    };
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                IsAvailable = true;
                return new LLMCompletionResponse
                {
                    Success = true,
                    Content = root.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var c)
                        ? (c.GetString() ?? string.Empty)
                        : string.Empty,
                    PromptTokens = root.TryGetProperty("prompt_eval_count", out var pt) ? pt.GetInt32() : 0,
                    CompletionTokens = root.TryGetProperty("eval_count", out var ct) ? ct.GetInt32() : 0,
                    FinishReason = "stop"
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                return new LLMCompletionResponse { Success = false, Error = ex.Message };
            }
        }

        public async IAsyncEnumerable<string> CompleteStreamAsync(
            LLMCompletionRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var payload = BuildPayload(request, stream: true);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat") { Content = content };
            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            // Ollama streams NDJSON: one JSON object per line.
            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line)) continue;

                string? chunk = null;
                bool done = false;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("done", out var d) && d.GetBoolean())
                        done = true;
                    if (root.TryGetProperty("message", out var msg) &&
                        msg.TryGetProperty("content", out var c))
                    {
                        chunk = c.GetString();
                    }
                }
                catch
                {
                    // Skip malformed line.
                }

                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk!;
                if (done)
                    yield break;
            }

            IsAvailable = true;
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cancellationToken);
                IsAvailable = response.IsSuccessStatusCode;
                return IsAvailable;
            }
            catch
            {
                IsAvailable = false;
                return false;
            }
        }

        private string BuildPayload(LLMCompletionRequest request, bool stream)
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? _defaultModel : request.Model;
            return ChatPayloadWriter.BuildOllama(model, request.Messages, request.Temperature, request.MaxTokens, stream);
        }
    }
}
