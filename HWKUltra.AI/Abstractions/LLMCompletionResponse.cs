namespace HWKUltra.AI.Abstractions
{
    public class LLMCompletionResponse
    {
        public string Content { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public string? FinishReason { get; set; }
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
    }
}
