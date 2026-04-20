namespace HWKUltra.AI.Abstractions
{
    public class LLMCompletionRequest
    {
        /// <summary>
        /// Model name. If null or empty, the provider uses its configured default model.
        /// </summary>
        public string? Model { get; set; }

        public List<LLMMessage> Messages { get; set; } = new();

        public float Temperature { get; set; } = 0.7f;

        public int MaxTokens { get; set; } = 2048;

        public Dictionary<string, object>? AdditionalParameters { get; set; }
    }
}
