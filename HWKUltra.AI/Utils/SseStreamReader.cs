using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HWKUltra.AI.Utils
{
    /// <summary>
    /// Parses OpenAI-compatible Server-Sent Events (SSE) streams and yields content chunks.
    /// Used by OpenAI, Azure OpenAI, and LM Studio services.
    /// </summary>
    public static class SseStreamReader
    {
        public static async IAsyncEnumerable<string> ReadContentChunksAsync(
            Stream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

                var payload = line.Substring(5).TrimStart();
                if (payload == "[DONE]") yield break;

                string? chunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(payload);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("choices", out var choices)) continue;
                    if (choices.GetArrayLength() == 0) continue;

                    var first = choices[0];
                    if (first.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var contentProp))
                    {
                        chunk = contentProp.GetString();
                    }
                }
                catch
                {
                    // Skip malformed line.
                    continue;
                }

                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk!;
            }
        }
    }
}
