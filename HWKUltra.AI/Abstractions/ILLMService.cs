namespace HWKUltra.AI.Abstractions
{
    /// <summary>
    /// Unified interface for both local and online LLM providers.
    /// </summary>
    public interface ILLMService
    {
        LLMProviderType ProviderType { get; }

        /// <summary>
        /// Whether this service is currently considered available.
        /// Updated by last successful/failed call and by <see cref="TestConnectionAsync"/>.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Non-streaming completion. Returns full response text.
        /// </summary>
        Task<LLMCompletionResponse> CompleteAsync(
            LLMCompletionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Streaming completion. Yields content chunks as they arrive.
        /// </summary>
        IAsyncEnumerable<string> CompleteStreamAsync(
            LLMCompletionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Probe connectivity. Updates <see cref="IsAvailable"/>.
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }
}
