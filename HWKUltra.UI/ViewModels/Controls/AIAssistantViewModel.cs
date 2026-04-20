using System.Collections.ObjectModel;
using HWKUltra.AI.Abstractions;
using HWKUltra.AI.Services;
using HWKUltra.UI.ViewModels.Pages;

namespace HWKUltra.UI.ViewModels.Controls
{
    /// <summary>
    /// ViewModel for the embeddable AI assistant panel.
    /// Lighter-weight than AIChatViewModel; intended for Creator/other pages with context injection.
    /// </summary>
    public partial class AIAssistantViewModel : ObservableObject
    {
        private readonly LLMServiceFactory _factory;
        private CancellationTokenSource? _cts;

        public AIAssistantViewModel(LLMServiceFactory factory)
        {
            _factory = factory;
            AvailableProviders = new ObservableCollection<LLMProviderType>(_factory.AvailableProviders);
            if (AvailableProviders.Count > 0)
            {
                SelectedProvider = _factory.Config.DefaultProvider;
                if (!AvailableProviders.Contains(SelectedProvider))
                    SelectedProvider = AvailableProviders[0];
            }
        }

        /// <summary>
        /// Optional context string injected as a system message prefix (e.g. current flow JSON, selected node info).
        /// Set by the host ViewModel before calling Send.
        /// </summary>
        public Func<string?>? ContextProvider { get; set; }

        [ObservableProperty]
        private ObservableCollection<LLMProviderType> _availableProviders = new();

        [ObservableProperty]
        private LLMProviderType _selectedProvider;

        [ObservableProperty]
        private ObservableCollection<ChatMessageItem> _messages = new();

        [ObservableProperty]
        private string _userInput = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [RelayCommand]
        private async Task SendAsync()
        {
            var text = UserInput?.Trim();
            if (string.IsNullOrEmpty(text) || IsBusy) return;
            if (AvailableProviders.Count == 0)
            {
                StatusMessage = "No provider configured.";
                return;
            }

            UserInput = string.Empty;
            Messages.Add(new ChatMessageItem { Role = LLMMessageRole.User, Content = text });

            var assistantMsg = new ChatMessageItem { Role = LLMMessageRole.Assistant, Content = string.Empty };
            Messages.Add(assistantMsg);

            IsBusy = true;
            _cts = new CancellationTokenSource();

            try
            {
                var service = _factory.GetService(SelectedProvider);
                var messages = new List<LLMMessage>();

                var ctx = ContextProvider?.Invoke();
                if (!string.IsNullOrWhiteSpace(ctx))
                    messages.Add(new LLMMessage(LLMMessageRole.System, ctx));

                foreach (var m in Messages.Take(Messages.Count - 1))
                {
                    if (!string.IsNullOrEmpty(m.Content))
                        messages.Add(new LLMMessage(m.Role, m.Content));
                }

                var request = new LLMCompletionRequest
                {
                    Messages = messages,
                    Temperature = 0.7f,
                    MaxTokens = 1500
                };

                StatusMessage = $"Streaming from {service.ProviderType}...";

                await foreach (var chunk in service.CompleteStreamAsync(request, _cts.Token))
                {
                    assistantMsg.Content += chunk;
                }

                StatusMessage = "Done.";
            }
            catch (OperationCanceledException)
            {
                assistantMsg.Content += "\n[Cancelled]";
                StatusMessage = "Cancelled.";
            }
            catch (Exception ex)
            {
                assistantMsg.Content = $"Error: {ex.Message}";
                StatusMessage = "Request failed.";
            }
            finally
            {
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        [RelayCommand]
        private void Cancel() => _cts?.Cancel();

        [RelayCommand]
        private void Clear()
        {
            Messages.Clear();
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// Programmatically set the input text and immediately send. Used by Quick Action buttons.
        /// </summary>
        public async Task AskAsync(string prompt)
        {
            UserInput = prompt;
            await SendAsync();
        }
    }
}
