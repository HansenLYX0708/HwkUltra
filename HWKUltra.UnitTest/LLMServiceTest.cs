using System.Net;
using System.Text;
using HWKUltra.AI.Abstractions;
using HWKUltra.AI.Config;
using HWKUltra.AI.Services;
using HWKUltra.AI.Services.Local;
using HWKUltra.AI.Services.Online;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Tests for the HWKUltra.AI layer: config, factory, and provider implementations.
    /// Uses a MockHttpMessageHandler to avoid real network calls.
    /// </summary>
    public static class LLMServiceTest
    {
        public static async Task RunAllTests()
        {
            var counter = new TestCounter();

            await Run("Config_LoadDefault_When_File_Missing", Test_Config_LoadDefault, counter);
            await Run("Config_RoundTrip_Save_Load", Test_Config_RoundTrip, counter);
            await Run("Factory_Only_Initializes_Enabled_Providers", Test_Factory_EnabledOnly, counter);
            await Run("Factory_Fallback_To_Online_When_Local_Missing", Test_Factory_Fallback, counter);
            await Run("Ollama_Complete_Parses_NativeResponse", Test_Ollama_Complete, counter);
            await Run("Ollama_Stream_Parses_NDJSON", Test_Ollama_Stream, counter);
            await Run("LMStudio_Complete_Parses_OpenAIFormat", Test_LMStudio_Complete, counter);
            await Run("OpenAI_Stream_Handles_SSE_And_Done", Test_OpenAI_Stream, counter);
            await Run("AzureOpenAI_Uses_ApiKey_Header_And_DeploymentUrl", Test_AzureOpenAI_Url, counter);
            await Run("OpenAICompatible_NonSuccess_Returns_ErrorResponse", Test_ErrorResponse, counter);

            Console.WriteLine($"\n[LLMServiceTest] {counter.Passed} passed, {counter.Failed} failed\n");
        }

        private class TestCounter
        {
            public int Passed;
            public int Failed;
        }

        // ===== Tests =====

        private static Task Test_Config_LoadDefault()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"llm_test_missing_{Guid.NewGuid():N}.json");
            var cfg = LLMConfigLoader.Load(tempPath);
            Assert(cfg != null, "Default config should not be null");
            Assert(cfg!.LocalOllama != null, "Default should have LocalOllama section");
            Assert(cfg.DefaultProvider == LLMProviderType.LocalOllama, "Default provider should be LocalOllama");
            return Task.CompletedTask;
        }

        private static Task Test_Config_RoundTrip()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"llm_test_roundtrip_{Guid.NewGuid():N}.json");
            try
            {
                var original = new LLMConfig
                {
                    DefaultProvider = LLMProviderType.OpenAI,
                    FallbackToOnline = true,
                    LocalOllama = new LocalProviderConfig { BaseUrl = "http://host:1", DefaultModel = "m1", Enabled = false },
                    OpenAI = new OnlineProviderConfig { ApiKey = "sk-test", DefaultModel = "gpt-test", Enabled = true }
                };
                LLMConfigLoader.Save(original, tempPath);
                var loaded = LLMConfigLoader.Load(tempPath);
                Assert(loaded.DefaultProvider == LLMProviderType.OpenAI, "Provider should round-trip");
                Assert(loaded.FallbackToOnline, "Fallback flag should round-trip");
                Assert(loaded.OpenAI?.ApiKey == "sk-test", "ApiKey should round-trip");
                Assert(loaded.OpenAI?.DefaultModel == "gpt-test", "Model should round-trip");
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            return Task.CompletedTask;
        }

        private static Task Test_Factory_EnabledOnly()
        {
            var cfg = new LLMConfig
            {
                DefaultProvider = LLMProviderType.LocalOllama,
                FallbackToOnline = false,
                LocalOllama = new LocalProviderConfig { BaseUrl = "http://x", DefaultModel = "m", Enabled = true },
                LocalLMStudio = new LocalProviderConfig { Enabled = false },
                OpenAI = new OnlineProviderConfig { Enabled = false },
                AzureOpenAI = new AzureProviderConfig { Enabled = false }
            };
            var factory = new LLMServiceFactory(cfg);
            var providers = factory.AvailableProviders.ToList();
            Assert(providers.Count == 1, $"Expected 1 provider, got {providers.Count}");
            Assert(providers[0] == LLMProviderType.LocalOllama, "Only LocalOllama should be available");
            return Task.CompletedTask;
        }

        private static Task Test_Factory_Fallback()
        {
            var cfg = new LLMConfig
            {
                DefaultProvider = LLMProviderType.LocalOllama,
                FallbackToOnline = true,
                LocalOllama = new LocalProviderConfig { Enabled = false },  // not configured
                OpenAI = new OnlineProviderConfig { ApiKey = "sk-x", DefaultModel = "gpt", Enabled = true }
            };
            var factory = new LLMServiceFactory(cfg);
            var svc = factory.GetService(LLMProviderType.LocalOllama);
            Assert(svc.ProviderType == LLMProviderType.OpenAI, $"Expected fallback to OpenAI, got {svc.ProviderType}");
            return Task.CompletedTask;
        }

        private static async Task Test_Ollama_Complete()
        {
            var handler = new MockHttpMessageHandler((req, ct) =>
            {
                Assert(req.RequestUri!.AbsolutePath == "/api/chat", $"Unexpected path: {req.RequestUri.AbsolutePath}");
                var body = """
                    {"model":"llama3","message":{"role":"assistant","content":"Hello from Ollama"},"done":true,"prompt_eval_count":5,"eval_count":10}
                    """;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            });
            var svc = new OllamaService("http://localhost:11434", "llama3", new HttpClient(handler));
            var resp = await svc.CompleteAsync(new LLMCompletionRequest
            {
                Messages = new List<LLMMessage> { new(LLMMessageRole.User, "Hi") }
            });
            Assert(resp.Success, $"Expected success, error={resp.Error}");
            Assert(resp.Content == "Hello from Ollama", $"Unexpected content: {resp.Content}");
            Assert(resp.PromptTokens == 5, "PromptTokens should be 5");
            Assert(resp.CompletionTokens == 10, "CompletionTokens should be 10");
        }

        private static async Task Test_Ollama_Stream()
        {
            var handler = new MockHttpMessageHandler((req, ct) =>
            {
                // Ollama NDJSON streaming
                var body = string.Join("\n", new[]
                {
                    "{\"message\":{\"content\":\"Hello \"},\"done\":false}",
                    "{\"message\":{\"content\":\"world\"},\"done\":false}",
                    "{\"message\":{\"content\":\"!\"},\"done\":false}",
                    "{\"done\":true}"
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/x-ndjson")
                };
            });
            var svc = new OllamaService("http://localhost:11434", "llama3", new HttpClient(handler));
            var chunks = new List<string>();
            await foreach (var c in svc.CompleteStreamAsync(new LLMCompletionRequest
            {
                Messages = new List<LLMMessage> { new(LLMMessageRole.User, "Hi") }
            }))
            {
                chunks.Add(c);
            }
            var joined = string.Concat(chunks);
            Assert(joined == "Hello world!", $"Unexpected stream content: '{joined}'");
        }

        private static async Task Test_LMStudio_Complete()
        {
            var handler = new MockHttpMessageHandler((req, ct) =>
            {
                Assert(req.RequestUri!.AbsolutePath == "/v1/chat/completions", $"Unexpected path: {req.RequestUri.AbsolutePath}");
                var body = """
                    {"choices":[{"message":{"role":"assistant","content":"LM Studio reply"},"finish_reason":"stop"}],
                     "usage":{"prompt_tokens":3,"completion_tokens":7,"total_tokens":10}}
                    """;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            });
            var svc = new LMStudioService("http://localhost:1234", "local", new HttpClient(handler));
            var resp = await svc.CompleteAsync(new LLMCompletionRequest
            {
                Messages = new List<LLMMessage> { new(LLMMessageRole.User, "Hi") }
            });
            Assert(resp.Success, $"Expected success, error={resp.Error}");
            Assert(resp.Content == "LM Studio reply", $"Unexpected content: {resp.Content}");
            Assert(resp.TotalTokens == 10, "TotalTokens should be 10");
            Assert(resp.FinishReason == "stop", "FinishReason should be 'stop'");
        }

        private static async Task Test_OpenAI_Stream()
        {
            var handler = new MockHttpMessageHandler((req, ct) =>
            {
                Assert(req.Headers.Authorization?.ToString() == "Bearer sk-test", "Authorization header should be set");
                var body = string.Join("\n", new[]
                {
                    "data: {\"choices\":[{\"delta\":{\"content\":\"A\"}}]}",
                    "data: {\"choices\":[{\"delta\":{\"content\":\"B\"}}]}",
                    "data: {\"choices\":[{\"delta\":{\"content\":\"C\"}}]}",
                    "data: [DONE]"
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
                };
            });
            var svc = new OpenAIService("sk-test", "gpt-test", "https://api.openai.com", new HttpClient(handler));
            var chunks = new List<string>();
            await foreach (var c in svc.CompleteStreamAsync(new LLMCompletionRequest
            {
                Messages = new List<LLMMessage> { new(LLMMessageRole.User, "Hi") }
            }))
            {
                chunks.Add(c);
            }
            Assert(string.Concat(chunks) == "ABC", $"Unexpected stream content: '{string.Concat(chunks)}'");
        }

        private static async Task Test_AzureOpenAI_Url()
        {
            string? capturedPath = null;
            bool hasApiKey = false;
            var handler = new MockHttpMessageHandler((req, ct) =>
            {
                capturedPath = req.RequestUri!.PathAndQuery;
                hasApiKey = req.Headers.Contains("api-key");
                var body = """
                    {"choices":[{"message":{"role":"assistant","content":"Azure"},"finish_reason":"stop"}],
                     "usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}
                    """;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            });
            var svc = new AzureOpenAIService("https://myres.openai.azure.com", "azkey", "mydep", "2024-02-15-preview", new HttpClient(handler));
            var resp = await svc.CompleteAsync(new LLMCompletionRequest
            {
                Messages = new List<LLMMessage> { new(LLMMessageRole.User, "Hi") }
            });
            Assert(resp.Success, $"Expected success, error={resp.Error}");
            Assert(capturedPath != null && capturedPath.Contains("/openai/deployments/mydep/chat/completions"), $"URL path wrong: {capturedPath}");
            Assert(capturedPath!.Contains("api-version=2024-02-15-preview"), $"Missing api-version: {capturedPath}");
            Assert(hasApiKey, "api-key header should be present");
        }

        private static async Task Test_ErrorResponse()
        {
            var handler = new MockHttpMessageHandler((req, ct) =>
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"error\":\"Invalid API key\"}", Encoding.UTF8, "application/json")
                };
            });
            var svc = new OpenAIService("sk-bad", "gpt", "https://api.openai.com", new HttpClient(handler));
            var resp = await svc.CompleteAsync(new LLMCompletionRequest
            {
                Messages = new List<LLMMessage> { new(LLMMessageRole.User, "Hi") }
            });
            Assert(!resp.Success, "Should be failure");
            Assert(resp.Error != null && resp.Error.Contains("401"), $"Error should mention 401: {resp.Error}");
            Assert(!svc.IsAvailable, "IsAvailable should be false after error");
        }

        // ===== Helpers =====

        private static async Task Run(string name, Func<Task> test, TestCounter counter)
        {
            try
            {
                await test();
                Console.WriteLine($"  [PASS] {name}");
                counter.Passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
                counter.Failed++;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        /// <summary>
        /// Simple HttpMessageHandler that routes all requests to a user-supplied delegate.
        /// </summary>
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;

            public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responder(request, cancellationToken));
            }
        }
    }
}
