using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HWKUltra.Flow.Models;
using HWKUltra.Creator.Models;
using HWKUltra.Creator.Services;

namespace HWKUltra.Creator.ViewModels
{
    /// <summary>
    /// ViewModel for the flow canvas (nodes + connections)
    /// </summary>
    public partial class FlowCanvasViewModel : ObservableObject
    {
        private readonly NodeCatalogService _catalogService;

        [ObservableProperty]
        private FlowNodeViewModel? _selectedNode;

        [ObservableProperty]
        private FlowConnectionViewModel? _selectedConnection;

        [ObservableProperty]
        private double _zoom = 1.0;

        [ObservableProperty]
        private double _offsetX;

        [ObservableProperty]
        private double _offsetY;

        [ObservableProperty]
        private string? _startNodeId;

        public ObservableCollection<FlowNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<FlowConnectionViewModel> Connections { get; } = new();

        public FlowCanvasViewModel(NodeCatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        /// <summary>
        /// Load from a FlowDefinition
        /// </summary>
        public void LoadFromDefinition(FlowDefinition definition)
        {
            Nodes.Clear();
            Connections.Clear();
            StartNodeId = definition.StartNodeId;

            // Create node VMs
            foreach (var nodeDef in definition.Nodes)
            {
                var entry = _catalogService.FindEntry(nodeDef.Type);
                var nodeVm = FlowNodeViewModel.FromDefinition(nodeDef, entry);
                nodeVm.IsStartNode = nodeDef.Id == definition.StartNodeId;
                Nodes.Add(nodeVm);
            }

            // Create connection VMs and resolve node references
            foreach (var connDef in definition.Connections)
            {
                var connVm = FlowConnectionViewModel.FromDefinition(connDef);
                connVm.SourceNode = Nodes.FirstOrDefault(n => n.Id == connDef.SourceNodeId);
                connVm.TargetNode = Nodes.FirstOrDefault(n => n.Id == connDef.TargetNodeId);
                Connections.Add(connVm);
            }
        }

        /// <summary>
        /// Export to FlowDefinition for serialization
        /// </summary>
        public FlowDefinition ToDefinition(string name = "Flow", string? description = null)
        {
            var definition = new FlowDefinition
            {
                Name = name,
                Description = description,
                StartNodeId = StartNodeId,
                ModifiedAt = DateTime.Now
            };

            foreach (var node in Nodes)
                definition.Nodes.Add(node.ToDefinition());

            foreach (var conn in Connections)
                definition.Connections.Add(conn.ToDefinition());

            return definition;
        }

        /// <summary>
        /// Add a new node at specified canvas position
        /// </summary>
        public FlowNodeViewModel AddNode(NodeCatalogEntry entry, double x, double y)
        {
            var nodeDef = new NodeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Type = entry.Type,
                Name = entry.DisplayName,
                X = x,
                Y = y
            };

            // Populate default properties from input definitions
            foreach (var input in entry.InputDefinitions)
            {
                if (input.DefaultValue != null)
                    nodeDef.Properties[input.Name] = input.DefaultValue.ToString() ?? string.Empty;
            }

            var nodeVm = FlowNodeViewModel.FromDefinition(nodeDef, entry);
            Nodes.Add(nodeVm);

            // If first node, set as start
            if (Nodes.Count == 1)
            {
                StartNodeId = nodeVm.Id;
                nodeVm.IsStartNode = true;
            }

            return nodeVm;
        }

        /// <summary>
        /// Add a connection between two nodes
        /// </summary>
        public FlowConnectionViewModel? AddConnection(string sourceNodeId, string targetNodeId, string? condition = null)
        {
            // Prevent duplicate connections
            if (Connections.Any(c => c.SourceNodeId == sourceNodeId && c.TargetNodeId == targetNodeId))
                return null;

            // Prevent self-connections
            if (sourceNodeId == targetNodeId)
                return null;

            var sourceNode = Nodes.FirstOrDefault(n => n.Id == sourceNodeId);
            var targetNode = Nodes.FirstOrDefault(n => n.Id == targetNodeId);
            if (sourceNode == null || targetNode == null)
                return null;

            var conn = new FlowConnectionViewModel
            {
                Id = Guid.NewGuid().ToString(),
                SourceNodeId = sourceNodeId,
                TargetNodeId = targetNodeId,
                Condition = condition,
                SourceNode = sourceNode,
                TargetNode = targetNode
            };

            Connections.Add(conn);
            return conn;
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            if (SelectedConnection != null)
            {
                Connections.Remove(SelectedConnection);
                SelectedConnection = null;
                return;
            }

            if (SelectedNode != null)
            {
                // Remove all connections to/from this node
                var related = Connections
                    .Where(c => c.SourceNodeId == SelectedNode.Id || c.TargetNodeId == SelectedNode.Id)
                    .ToList();
                foreach (var conn in related)
                    Connections.Remove(conn);

                // If this was the start node, reassign
                if (SelectedNode.IsStartNode && Nodes.Count > 1)
                {
                    var next = Nodes.FirstOrDefault(n => n.Id != SelectedNode.Id);
                    if (next != null)
                    {
                        StartNodeId = next.Id;
                        next.IsStartNode = true;
                    }
                }

                Nodes.Remove(SelectedNode);
                SelectedNode = null;
            }
        }

        [RelayCommand]
        private void SetStartNode(FlowNodeViewModel node)
        {
            foreach (var n in Nodes)
                n.IsStartNode = false;

            node.IsStartNode = true;
            StartNodeId = node.Id;
        }

        [RelayCommand]
        private void SelectNode(FlowNodeViewModel? node)
        {
            foreach (var n in Nodes)
                n.IsSelected = false;
            foreach (var c in Connections)
                c.IsSelected = false;

            SelectedConnection = null;

            if (node != null)
            {
                node.IsSelected = true;
                SelectedNode = node;
            }
            else
            {
                SelectedNode = null;
            }
        }

        [RelayCommand]
        private void SelectConnection(FlowConnectionViewModel? connection)
        {
            foreach (var n in Nodes)
                n.IsSelected = false;
            foreach (var c in Connections)
                c.IsSelected = false;

            SelectedNode = null;

            if (connection != null)
            {
                connection.IsSelected = true;
                SelectedConnection = connection;
            }
            else
            {
                SelectedConnection = null;
            }
        }
    }
}
