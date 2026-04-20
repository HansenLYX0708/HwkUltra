using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HWKUltra.UI.ViewModels.Controls;

namespace HWKUltra.UI.Views.Controls
{
    public partial class AIAssistantPanel : UserControl
    {
        public AIAssistantPanel()
        {
            InitializeComponent();
        }

        private void UserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (DataContext is AIAssistantViewModel vm && vm.SendCommand.CanExecute(null))
                    vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }

        private async void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            if (DataContext is not AIAssistantViewModel vm) return;

            var prompt = fe.Tag?.ToString() switch
            {
                "explain" => "Please explain the currently selected node in the flow, including its purpose and key parameters.",
                "optimize" => "Please analyze the current flow and suggest optimizations for performance and reliability.",
                "errors" => "Please check the current flow for potential errors, missing connections, or misconfigured nodes.",
                _ => null
            };

            if (!string.IsNullOrEmpty(prompt))
                await vm.AskAsync(prompt);
        }
    }
}
