using HWKUltra.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HWKUltra.UI.Views.Pages
{
    public partial class FlowRunnerPage : INavigableView<FlowRunnerViewModel>
    {
        public FlowRunnerViewModel ViewModel { get; }

        public FlowRunnerPage(FlowRunnerViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();

            // Auto-scroll log to bottom
            ViewModel.LogEntries.CollectionChanged += (_, _) =>
            {
                if (LogListBox.Items.Count > 0)
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            };
        }
    }
}
