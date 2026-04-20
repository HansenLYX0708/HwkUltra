using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HWKUltra.AI.Abstractions;
using HWKUltra.AI.Utils;

namespace HWKUltra.AI.Services
{
    /// <summary>
    /// Base class for any service that exposes an OpenAI-compatible
    /// /chat/completions endpoint (OpenAI, Azure OpenAI, LM Studio, vLLM, etc.).
    /// </summary>
    public abstract class OpenAICompatibleServiceBase : ILLMService
    {
        protected readonly HttpClient HttpClient;

        public abstract LLMProviderType ProviderType { get; }
        public bool IsAvailable { get; protected set; } = true;

        protected OpenAICompatibleServiceBase(HttpClient? httpClient = null)
        {
            HttpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        }

        /// <summary>Full URL for the chat completions endpoint.</summary>
        protected abstract string ChatCompletionsUrl { get; }

        /// <summary>Full URL for the connection test (e.g. list models).</summary>
        protected abstract string TestConnectionUrl { get; }

        protected abstract string DefaultModel { get; }

        /// <summary>
        /// Whether the payload should include the "model" field. Azure OpenAI sets it from deployment URL instead.
        /// </summary>
        protected virtual bool IncludeModelInPayload => true;

        public async Task<LLMCompletionResponse> CompleteAsync(LLMCompletionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = BuildPayload(request, stream: false);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await HttpClient.PostAsync(ChatCompletionsUrl, content, cancellationToken);

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

                var choices = root.GetProperty("choices");
                var first = choices[0];
                var message = first.GetProperty("message");
                var contentStr = message.TryGetProperty("content", out var c) ? (c.GetString() ?? string.Empty) : string.Empty;
                var finish = first.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;

                int pt = 0, ct = 0, tt = 0;
                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var p)) pt = p.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var cc)) ct = cc.GetInt32();
                    if (usage.TryGetProperty("total_tokens", out var t)) tt = t.GetInt32();
                }

                IsAvailable = true;
                return new LLMCompletionResponse
                {
                    Success = true,
                    Content = contentStr,
                    PromptTokens = pt,
                    CompletionTokens = ct,
                    TotalTokens = tt,
                    FinishReason = finish
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
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl) { Content = content };
            using var response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var chunk in SseStreamReader.ReadContentChunksAsync(stream, cancellationToken))
            {
                yield return chunk;
            }

            IsAvailable = true;
        }

        public virtual async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await HttpClient.GetAsync(TestConnectionUrl, cancellationToken);
                IsAvailable = response.IsSuccessStatusCode;
                return IsAvailable;
            }
            catch
            {
                IsAvailable = false;
                return false;
            }
        }

        protected virtual string BuildPayload(LLMCompletionRequest request, bool stream)
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? DefaultModel : request.Model;
            var modelForPayload = IncludeModelInPayload ? model : null;
            return ChatPayloadWriter.BuildOpenAICompatible(
                modelForPayload, request.Messages, request.Temperature, request.MaxTokens, stream);
        }
    }
}
