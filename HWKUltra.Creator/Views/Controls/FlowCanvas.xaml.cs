using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HWKUltra.Creator.Models;
using HWKUltra.Creator.ViewModels;

namespace HWKUltra.Creator.Views.Controls
{
    public partial class FlowCanvas : UserControl
    {
        private FlowCanvasViewModel? ViewModel => DataContext as FlowCanvasViewModel;

        // Node dragging state
        private bool _isDraggingNode;
        private FlowNodeViewModel? _draggingNode;
        private Point _dragStartMouse;
        private double _dragStartX;
        private double _dragStartY;

        // Connection dragging state
        private bool _isDraggingConnection;
        private FlowNodeViewModel? _connectionSourceNode;
        private Point _connectionStartPoint;

        public FlowCanvas()
        {
            InitializeComponent();
        }

        #region Drop from Toolbox

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("NodeCatalogEntry"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("NodeCatalogEntry") && ViewModel != null)
            {
                var entry = e.Data.GetData("NodeCatalogEntry") as NodeCatalogEntry;
                if (entry != null)
                {
                    var pos = e.GetPosition(CanvasRoot);
                    ViewModel.AddNode(entry, pos.X - 80, pos.Y - 40);
                }
            }
        }

        #endregion

        #region Node Selection & Dragging

        private void Node_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FlowNodeViewModel nodeVm)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    // Select node
                    ViewModel?.SelectNodeCommand.Execute(nodeVm);

                    // Start drag
                    _isDraggingNode = true;
                    _draggingNode = nodeVm;
                    _dragStartMouse = e.GetPosition(CanvasRoot);
                    _dragStartX = nodeVm.X;
                    _dragStartY = nodeVm.Y;
                    element.CaptureMouse();
                    e.Handled = true;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    // Right-click: set as start node
                    ViewModel?.SetStartNodeCommand.Execute(nodeVm);
                    e.Handled = true;
                }
            }
        }

        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingNode && _draggingNode != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(CanvasRoot);
                var dx = currentPos.X - _dragStartMouse.X;
                var dy = currentPos.Y - _dragStartMouse.Y;

                _draggingNode.X = Math.Max(0, _dragStartX + dx);
                _draggingNode.Y = Math.Max(0, _dragStartY + dy);
                e.Handled = true;
            }
        }

        private void Node_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingNode && sender is FrameworkElement element)
            {
                _isDraggingNode = false;
                _draggingNode = null;
                element.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion

        #region Connection Dragging

        private void OutputPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FlowNodeViewModel nodeVm)
            {
                _isDraggingConnection = true;
                _connectionSourceNode = nodeVm;
                _connectionStartPoint = new Point(
                    nodeVm.X + nodeVm.Width,
                    nodeVm.Y + nodeVm.Height / 2);

                TempConnectionPath.Visibility = Visibility.Visible;
                UpdateTempConnection(e.GetPosition(CanvasRoot));

                // Capture mouse at canvas level to track drag anywhere
                this.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Hit-test to find input port under mouse position
        /// </summary>
        private FlowNodeViewModel? FindInputPortAtPoint(Point point)
        {
            var hitResult = VisualTreeHelper.HitTest(CanvasRoot, point);
            if (hitResult?.VisualHit is FrameworkElement element)
            {
                // Walk up visual tree to find element with Tag = FlowNodeViewModel
                var current = element;
                while (current != null)
                {
                    if (current.Tag is FlowNodeViewModel nodeVm)
                    {
                        // Check if we hit the input port (left ellipse)
                        // We accept any hit on the node during connection drag
                        return nodeVm;
                    }
                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
            }
            return null;
        }

        private void UpdateTempConnection(Point mousePos)
        {
            if (!_isDraggingConnection) return;

            var sx = _connectionStartPoint.X;
            var sy = _connectionStartPoint.Y;
            var tx = mousePos.X;
            var ty = mousePos.Y;
            var dx = Math.Abs(tx - sx) * 0.5;
            if (dx < 30) dx = 30;

            var pathData = $"M {sx.ToString(CultureInfo.InvariantCulture)},{sy.ToString(CultureInfo.InvariantCulture)} " +
                          $"C {(sx + dx).ToString(CultureInfo.InvariantCulture)},{sy.ToString(CultureInfo.InvariantCulture)} " +
                          $"{(tx - dx).ToString(CultureInfo.InvariantCulture)},{ty.ToString(CultureInfo.InvariantCulture)} " +
                          $"{tx.ToString(CultureInfo.InvariantCulture)},{ty.ToString(CultureInfo.InvariantCulture)}";

            TempConnectionPath.Data = Geometry.Parse(pathData);
        }

        private void EndConnectionDrag()
        {
            _isDraggingConnection = false;
            _connectionSourceNode = null;
            TempConnectionPath.Visibility = Visibility.Collapsed;
            TempConnectionPath.Data = null;
        }

        #endregion

        #region Connection Selection

        private void Connection_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FlowConnectionViewModel connVm)
            {
                ViewModel?.SelectConnectionCommand.Execute(connVm);
                e.Handled = true;
            }
        }

        #endregion

        #region Canvas Events

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Cancel connection drag on any canvas click (except when clicking output port to start new drag)
            if (_isDraggingConnection)
            {
                EndConnectionDrag();
                this.ReleaseMouseCapture();
                e.Handled = true;
                return;
            }

            if (e.OriginalSource == this || e.OriginalSource is Grid)
            {
                // Click on empty canvas: deselect all
                ViewModel?.SelectNodeCommand.Execute(null);
                this.Focus();
            }
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                ViewModel?.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void CanvasRoot_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom is handled via ScrollViewer built-in scrolling
            // Future: could implement Ctrl+Wheel zoom here
        }

        #endregion

        /// <summary>
        /// Override OnMouseMove at canvas level for connection dragging
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDraggingConnection)
            {
                var pos = e.GetPosition(CanvasRoot);
                UpdateTempConnection(pos);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isDraggingConnection)
            {
                // Check if released over a target node
                var pos = e.GetPosition(CanvasRoot);
                var targetNode = FindInputPortAtPoint(pos);

                if (targetNode != null && targetNode != _connectionSourceNode)
                {
                    // Create connection
                    ViewModel?.AddConnection(_connectionSourceNode!.Id, targetNode.Id);
                }

                EndConnectionDrag();
                this.ReleaseMouseCapture();
            }
        }
    }
}
