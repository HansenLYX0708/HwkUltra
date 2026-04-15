using CommunityToolkit.Mvvm.ComponentModel;
using HWKUltra.Flow.Models;

namespace HWKUltra.Creator.ViewModels
{
    /// <summary>
    /// ViewModel for a connection line between two nodes
    /// </summary>
    public partial class FlowConnectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _sourceNodeId = string.Empty;

        [ObservableProperty]
        private string _targetNodeId = string.Empty;

        [ObservableProperty]
        private string? _condition;

        [ObservableProperty]
        private bool _isSelected;

        // Cached references for rendering
        private FlowNodeViewModel? _sourceNode;
        private FlowNodeViewModel? _targetNode;

        public FlowNodeViewModel? SourceNode
        {
            get => _sourceNode;
            set
            {
                _sourceNode = value;
                if (value != null)
                {
                    value.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName is nameof(FlowNodeViewModel.X) or nameof(FlowNodeViewModel.Y)
                            or nameof(FlowNodeViewModel.Width) or nameof(FlowNodeViewModel.Height))
                        {
                            OnPropertyChanged(nameof(SourceX));
                            OnPropertyChanged(nameof(SourceY));
                            OnPropertyChanged(nameof(PathData));
                        }
                    };
                }
            }
        }

        public FlowNodeViewModel? TargetNode
        {
            get => _targetNode;
            set
            {
                _targetNode = value;
                if (value != null)
                {
                    value.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName is nameof(FlowNodeViewModel.X) or nameof(FlowNodeViewModel.Y)
                            or nameof(FlowNodeViewModel.Width) or nameof(FlowNodeViewModel.Height))
                        {
                            OnPropertyChanged(nameof(TargetX));
                            OnPropertyChanged(nameof(TargetY));
                            OnPropertyChanged(nameof(PathData));
                        }
                    };
                }
            }
        }

        /// <summary>
        /// Source point: right-center of source node
        /// </summary>
        public double SourceX => (SourceNode?.X ?? 0) + (SourceNode?.Width ?? 160);
        public double SourceY => (SourceNode?.Y ?? 0) + (SourceNode?.Height ?? 80) / 2;

        /// <summary>
        /// Target point: left-center of target node
        /// </summary>
        public double TargetX => TargetNode?.X ?? 0;
        public double TargetY => (TargetNode?.Y ?? 0) + (TargetNode?.Height ?? 80) / 2;

        /// <summary>
        /// Midpoint for condition label placement
        /// </summary>
        public double MidX => (SourceX + TargetX) / 2;
        public double MidY => (SourceY + TargetY) / 2;

        /// <summary>
        /// Bezier path data string for rendering
        /// </summary>
        public string PathData
        {
            get
            {
                var sx = SourceX;
                var sy = SourceY;
                var tx = TargetX;
                var ty = TargetY;
                var dx = Math.Abs(tx - sx) * 0.5;
                return $"M {sx},{sy} C {sx + dx},{sy} {tx - dx},{ty} {tx},{ty}";
            }
        }

        /// <summary>
        /// Create from ConnectionDefinition
        /// </summary>
        public static FlowConnectionViewModel FromDefinition(ConnectionDefinition def)
        {
            return new FlowConnectionViewModel
            {
                Id = def.Id,
                SourceNodeId = def.SourceNodeId,
                TargetNodeId = def.TargetNodeId,
                Condition = def.Condition
            };
        }

        /// <summary>
        /// Convert back to ConnectionDefinition
        /// </summary>
        public ConnectionDefinition ToDefinition()
        {
            return new ConnectionDefinition
            {
                Id = Id,
                SourceNodeId = SourceNodeId,
                TargetNodeId = TargetNodeId,
                Condition = Condition
            };
        }
    }
}
