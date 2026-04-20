namespace HWKUltra.AI.Abstractions
{
    /// <summary>
    /// LLM provider implementations supported by the AI layer.
    /// </summary>
    public enum LLMProviderType
    {
        LocalOllama,
        LocalLMStudio,
        OpenAI,
        AzureOpenAI
    }
}
