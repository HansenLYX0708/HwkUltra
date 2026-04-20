using System.Text;
using System.Text.Json;
using HWKUltra.AI.Abstractions;

namespace HWKUltra.AI.Utils
{
    /// <summary>
    /// AOT-safe JSON payload builder for chat completion requests.
    /// Uses Utf8JsonWriter directly to avoid reflection-based serialization.
    /// </summary>
    public static class ChatPayloadWriter
    {
        /// <summary>
        /// Build an OpenAI-compatible chat completions payload.
        /// </summary>
        /// <param name="model">Model name, or null to omit the "model" field (used by Azure OpenAI).</param>
        /// <param name="messages">Conversation messages.</param>
        /// <param name="temperature">Sampling temperature.</param>
        /// <param name="maxTokens">Maximum output tokens.</param>
        /// <param name="stream">Enable streaming.</param>
        public static string BuildOpenAICompatible(string? model, IList<LLMMessage> messages, float temperature, int maxTokens, bool stream)
        {
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                if (!string.IsNullOrWhiteSpace(model))
                    writer.WriteString("model", model);

                writer.WritePropertyName("messages");
                writer.WriteStartArray();
                foreach (var m in messages)
                {
                    writer.WriteStartObject();
                    writer.WriteString("role", m.RoleString);
                    writer.WriteString("content", m.Content);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteNumber("temperature", temperature);
                writer.WriteNumber("max_tokens", maxTokens);
                writer.WriteBoolean("stream", stream);
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>
        /// Build an Ollama native chat payload (/api/chat).
        /// </summary>
        public static string BuildOllama(string model, IList<LLMMessage> messages, float temperature, int maxTokens, bool stream)
        {
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                writer.WriteString("model", model);

                writer.WritePropertyName("messages");
                writer.WriteStartArray();
                foreach (var m in messages)
                {
                    writer.WriteStartObject();
                    writer.WriteString("role", m.RoleString);
                    writer.WriteString("content", m.Content);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteBoolean("stream", stream);

                writer.WritePropertyName("options");
                writer.WriteStartObject();
                writer.WriteNumber("temperature", temperature);
                writer.WriteNumber("num_predict", maxTokens);
                writer.WriteEndObject();

                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
