using CommunityToolkit.Mvvm.ComponentModel;

namespace HWKUltra.Creator.ViewModels
{
    /// <summary>
    /// ViewModel for the property editing panel (bound to selected node)
    /// </summary>
    public partial class PropertyPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private FlowNodeViewModel? _selectedNode;

        [ObservableProperty]
        private FlowConnectionViewModel? _selectedConnection;

        [ObservableProperty]
        private bool _hasSelection;

        [ObservableProperty]
        private bool _isNodeSelected;

        [ObservableProperty]
        private bool _isConnectionSelected;

        public void UpdateSelection(FlowNodeViewModel? node, FlowConnectionViewModel? connection)
        {
            SelectedNode = node;
            SelectedConnection = connection;
            IsNodeSelected = node != null;
            IsConnectionSelected = connection != null;
            HasSelection = node != null || connection != null;
        }
    }
}
