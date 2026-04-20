using System.Collections.ObjectModel;
using HWKUltra.AI.Abstractions;
using HWKUltra.AI.Services;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.ViewModels.Pages
{
    /// <summary>
    /// ViewModel for the independent AI Assistant chat page.
    /// Supports provider switching, connection testing, streaming responses, and cancellation.
    /// </summary>
    public partial class AIChatViewModel : ObservableObject, INavigationAware
    {
        private readonly LLMServiceFactory _factory;
        private CancellationTokenSource? _cts;

        public AIChatViewModel(LLMServiceFactory factory)
        {
            _factory = factory;

            // Populate available providers from factory
            AvailableProviders = new ObservableCollection<LLMProviderType>(_factory.AvailableProviders);

            if (AvailableProviders.Count > 0)
            {
                SelectedProvider = _factory.Config.DefaultProvider;
                if (!AvailableProviders.Contains(SelectedProvider))
                    SelectedProvider = AvailableProviders[0];
            }

            StatusMessage = AvailableProviders.Count > 0
                ? $"Ready. {AvailableProviders.Count} provider(s) configured."
                : "No LLM provider configured. Edit ConfigJson/LLM/LLMConfig.json.";
        }

        public Task OnNavigatedToAsync() => Task.CompletedTask;
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        #region Properties

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

        [ObservableProperty]
        private string _modelOverride = string.Empty;

        #endregion

        #region Commands

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
                var request = new LLMCompletionRequest
                {
                    Model = string.IsNullOrWhiteSpace(ModelOverride) ? null : ModelOverride,
                    Messages = BuildMessageHistory(),
                    Temperature = 0.7f,
                    MaxTokens = 2048
                };

                StatusMessage = $"Streaming from {service.ProviderType}...";

                await foreach (var chunk in service.CompleteStreamAsync(request, _cts.Token))
                {
                    assistantMsg.Content += chunk;
                }

                StatusMessage = $"Done ({service.ProviderType}).";
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
        private void Cancel()
        {
            _cts?.Cancel();
        }

        [RelayCommand]
        private void Clear()
        {
            Messages.Clear();
            StatusMessage = "Conversation cleared.";
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (AvailableProviders.Count == 0)
            {
                StatusMessage = "No provider configured.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Testing connection...";
            try
            {
                var results = await _factory.TestAllConnectionsAsync();
                var lines = results.Select(r => $"{r.Key}: {(r.Value ? "OK" : "FAIL")}");
                StatusMessage = string.Join(" | ", lines);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        private List<LLMMessage> BuildMessageHistory()
        {
            // Only include prior user+assistant messages (exclude the currently-being-populated assistant reply).
            var list = new List<LLMMessage>();
            for (int i = 0; i < Messages.Count - 1; i++)
            {
                var m = Messages[i];
                if (!string.IsNullOrEmpty(m.Content))
                    list.Add(new LLMMessage(m.Role, m.Content));
            }
            return list;
        }
    }

    public partial class ChatMessageItem : ObservableObject
    {
        [ObservableProperty]
        private LLMMessageRole _role;

        [ObservableProperty]
        private string _content = string.Empty;

        public bool IsUser => Role == LLMMessageRole.User;
        public bool IsAssistant => Role == LLMMessageRole.Assistant;
        public string RoleDisplay => Role.ToString();
    }
}
