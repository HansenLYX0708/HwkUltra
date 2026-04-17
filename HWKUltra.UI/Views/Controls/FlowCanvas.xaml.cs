using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HWKUltra.UI.Models;
using HWKUltra.UI.ViewModels.Pages;

namespace HWKUltra.UI.Views.Controls
{
    public partial class FlowCanvas : UserControl
    {
        private CreatorViewModel? ViewModel => DataContext as CreatorViewModel;

        // Node dragging state
        private bool _isDraggingNode;
        private FlowNodeViewModel? _draggingNode;
        private Point _dragStartMouse;
        private double _dragStartX;
        private double _dragStartY;
        // Multi-drag: store initial positions of all selected nodes
        private Dictionary<FlowNodeViewModel, Point> _multiDragStartPositions = new();

        // Connection dragging state
        private bool _isDraggingConnection;
        private FlowNodeViewModel? _connectionSourceNode;
        private Point _connectionStartPoint;
        private bool _connectionFromInput; // true if dragging from input port (flipped direction)

        // Canvas panning state (middle-button or Space+left)
        private bool _isPanning;
        private Point _panStartMouse;
        private double _panStartHOffset;
        private double _panStartVOffset;
        private bool _spaceHeld;

        // Rubber-band selection state
        private bool _isRubberBandSelecting;
        private Point _rubberBandStart;

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
                    bool isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

                    // If node is already selected in a multi-selection, don't deselect others
                    if (!isCtrl && ViewModel != null && !ViewModel.SelectedNodes.Contains(nodeVm))
                        ViewModel.SelectNode(nodeVm, false);
                    else
                        ViewModel?.SelectNode(nodeVm, isCtrl);

                    // Start drag — store initial positions of all selected nodes
                    _isDraggingNode = true;
                    _draggingNode = nodeVm;
                    _dragStartMouse = e.GetPosition(CanvasRoot);
                    _dragStartX = nodeVm.X;
                    _dragStartY = nodeVm.Y;

                    _multiDragStartPositions.Clear();
                    if (ViewModel != null)
                    {
                        foreach (var n in ViewModel.SelectedNodes)
                            _multiDragStartPositions[n] = new Point(n.X, n.Y);
                    }

                    element.CaptureMouse();
                    e.Handled = true;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
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

                // Move all selected nodes together
                foreach (var kvp in _multiDragStartPositions)
                {
                    kvp.Key.X = Math.Max(0, kvp.Value.X + dx);
                    kvp.Key.Y = Math.Max(0, kvp.Value.Y + dy);
                }

                e.Handled = true;
            }
        }

        private void Node_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingNode && sender is FrameworkElement element)
            {
                _isDraggingNode = false;
                _draggingNode = null;
                _multiDragStartPositions.Clear();
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
                _connectionFromInput = false;

                // Output port position depends on flip state
                _connectionStartPoint = nodeVm.IsFlipped
                    ? new Point(nodeVm.X, nodeVm.Y + nodeVm.Height / 2)
                    : new Point(nodeVm.X + nodeVm.Width, nodeVm.Y + nodeVm.Height / 2);

                TempConnectionPath.Visibility = Visibility.Visible;
                UpdateTempConnection(e.GetPosition(CanvasRoot));

                this.CaptureMouse();
                e.Handled = true;
            }
        }

        private void InputPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FlowNodeViewModel nodeVm)
            {
                _isDraggingConnection = true;
                _connectionSourceNode = nodeVm;
                _connectionFromInput = true;

                // Input port position depends on flip state
                _connectionStartPoint = nodeVm.IsFlipped
                    ? new Point(nodeVm.X + nodeVm.Width, nodeVm.Y + nodeVm.Height / 2)
                    : new Point(nodeVm.X, nodeVm.Y + nodeVm.Height / 2);

                TempConnectionPath.Visibility = Visibility.Visible;
                UpdateTempConnection(e.GetPosition(CanvasRoot));

                this.CaptureMouse();
                e.Handled = true;
            }
        }

        private FlowNodeViewModel? FindInputPortAtPoint(Point point)
        {
            var hitResult = VisualTreeHelper.HitTest(CanvasRoot, point);
            if (hitResult?.VisualHit is FrameworkElement element)
            {
                var current = element;
                while (current != null)
                {
                    if (current.Tag is FlowNodeViewModel nodeVm)
                        return nodeVm;
                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
            }
            return null;
        }

        private void UpdateTempConnection(Point mousePos)
        {
            if (!_isDraggingConnection || _connectionSourceNode == null) return;

            var inv = CultureInfo.InvariantCulture;
            var sx = _connectionStartPoint.X;
            var sy = _connectionStartPoint.Y;
            var tx = mousePos.X;
            var ty = mousePos.Y;
            var dx = Math.Abs(tx - sx) * 0.5;
            if (dx < 50) dx = 50;

            // Determine source port facing direction
            double sDir;
            if (_connectionFromInput)
            {
                // Dragging from input port: normal input faces left (-1), flipped input faces right (+1)
                sDir = _connectionSourceNode.IsFlipped ? 1.0 : -1.0;
            }
            else
            {
                // Dragging from output port: normal output faces right (+1), flipped output faces left (-1)
                sDir = _connectionSourceNode.IsFlipped ? -1.0 : 1.0;
            }

            // Mouse end: control point goes opposite direction of source
            double tDir = -sDir;

            var c1x = sx + dx * sDir;
            var c2x = tx + dx * tDir;

            var pathData = $"M {sx.ToString(inv)},{sy.ToString(inv)} " +
                          $"C {c1x.ToString(inv)},{sy.ToString(inv)} " +
                          $"{c2x.ToString(inv)},{ty.ToString(inv)} " +
                          $"{tx.ToString(inv)},{ty.ToString(inv)}";

            TempConnectionPath.Data = Geometry.Parse(pathData);
        }

        private void EndConnectionDrag()
        {
            _isDraggingConnection = false;
            _connectionSourceNode = null;
            _connectionFromInput = false;
            TempConnectionPath.Visibility = Visibility.Collapsed;
            TempConnectionPath.Data = null;
        }

        #endregion

        #region Connection Selection

        private void Connection_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FlowConnectionViewModel connVm)
            {
                ViewModel?.SelectConnection(connVm);
                e.Handled = true;
            }
        }

        #endregion

        #region Canvas Panning

        private void StartPan(Point screenPos)
        {
            _isPanning = true;
            _panStartMouse = screenPos;
            _panStartHOffset = CanvasScrollViewer.HorizontalOffset;
            _panStartVOffset = CanvasScrollViewer.VerticalOffset;
            this.Cursor = Cursors.ScrollAll;
            this.CaptureMouse();
        }

        private void UpdatePan(Point screenPos)
        {
            if (!_isPanning) return;
            var dx = screenPos.X - _panStartMouse.X;
            var dy = screenPos.Y - _panStartMouse.Y;
            CanvasScrollViewer.ScrollToHorizontalOffset(_panStartHOffset - dx);
            CanvasScrollViewer.ScrollToVerticalOffset(_panStartVOffset - dy);
        }

        private void EndPan()
        {
            _isPanning = false;
            this.Cursor = Cursors.Arrow;
            this.ReleaseMouseCapture();
        }

        #endregion

        #region Rubber-Band Selection

        private void StartRubberBand(Point canvasPos)
        {
            _isRubberBandSelecting = true;
            _rubberBandStart = canvasPos;

            Canvas.SetLeft(SelectionRect, canvasPos.X);
            Canvas.SetTop(SelectionRect, canvasPos.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionRect.Visibility = Visibility.Visible;
        }

        private void UpdateRubberBand(Point canvasPos)
        {
            if (!_isRubberBandSelecting) return;

            var x = Math.Min(_rubberBandStart.X, canvasPos.X);
            var y = Math.Min(_rubberBandStart.Y, canvasPos.Y);
            var w = Math.Abs(canvasPos.X - _rubberBandStart.X);
            var h = Math.Abs(canvasPos.Y - _rubberBandStart.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        private void EndRubberBand(Point canvasPos)
        {
            _isRubberBandSelecting = false;
            SelectionRect.Visibility = Visibility.Collapsed;

            var x = Math.Min(_rubberBandStart.X, canvasPos.X);
            var y = Math.Min(_rubberBandStart.Y, canvasPos.Y);
            var w = Math.Abs(canvasPos.X - _rubberBandStart.X);
            var h = Math.Abs(canvasPos.Y - _rubberBandStart.Y);

            if (w > 5 && h > 5)
            {
                var rect = new Rect(x, y, w, h);
                bool isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                ViewModel?.SelectNodesInRect(rect, isCtrl);
            }
        }

        #endregion

        #region Canvas Events

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Cancel connection drag
            if (_isDraggingConnection)
            {
                EndConnectionDrag();
                this.ReleaseMouseCapture();
                e.Handled = true;
                return;
            }

            // Middle-button pan
            if (e.ChangedButton == MouseButton.Middle)
            {
                StartPan(e.GetPosition(this));
                e.Handled = true;
                return;
            }

            // Space+left: pan
            if (e.ChangedButton == MouseButton.Left && _spaceHeld)
            {
                StartPan(e.GetPosition(this));
                e.Handled = true;
                return;
            }

            // Left-click on empty canvas
            if (e.ChangedButton == MouseButton.Left)
            {
                // Check if we clicked on empty canvas (not on a node/connection)
                var hitResult = VisualTreeHelper.HitTest(CanvasRoot, e.GetPosition(CanvasRoot));
                bool hitNode = false;
                if (hitResult?.VisualHit is FrameworkElement fe)
                {
                    var current = fe;
                    while (current != null && current != CanvasRoot)
                    {
                        if (current.Tag is FlowNodeViewModel || current.Tag is FlowConnectionViewModel)
                        { hitNode = true; break; }
                        current = VisualTreeHelper.GetParent(current) as FrameworkElement;
                    }
                }

                if (!hitNode)
                {
                    // Start rubber-band selection on empty canvas
                    StartRubberBand(e.GetPosition(CanvasRoot));
                    if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        ViewModel?.SelectNode(null);
                    this.Focus();
                    this.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                ViewModel?.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ViewModel?.SelectAllCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.None)
            {
                ViewModel?.FlipSelectedCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                _spaceHeld = true;
                this.Cursor = Cursors.ScrollAll;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (_isDraggingConnection) { EndConnectionDrag(); this.ReleaseMouseCapture(); }
                if (_isRubberBandSelecting) { _isRubberBandSelecting = false; SelectionRect.Visibility = Visibility.Collapsed; this.ReleaseMouseCapture(); }
                ViewModel?.SelectNode(null);
                e.Handled = true;
            }
        }

        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                _spaceHeld = false;
                if (!_isPanning)
                    this.Cursor = Cursors.Arrow;
            }
        }

        private void CanvasRoot_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Handled by FlowCanvas_PreviewMouseWheel
        }

        private void FlowCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null) return;

            // Ctrl+Wheel: zoom
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                    ViewModel.ZoomInCommand.Execute(null);
                else
                    ViewModel.ZoomOutCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Shift+Wheel: horizontal scroll
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (e.Delta > 0)
                    CanvasScrollViewer.LineLeft();
                else
                    CanvasScrollViewer.LineRight();
                e.Handled = true;
                return;
            }

            // Wheel: vertical scroll
            if (CanvasScrollViewer != null)
            {
                if (e.Delta > 0)
                    CanvasScrollViewer.LineUp();
                else
                    CanvasScrollViewer.LineDown();
                e.Handled = true;
            }
        }

        #endregion

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isPanning)
            {
                UpdatePan(e.GetPosition(this));
                return;
            }

            if (_isDraggingConnection)
            {
                var pos = e.GetPosition(CanvasRoot);
                UpdateTempConnection(pos);
                return;
            }

            if (_isRubberBandSelecting)
            {
                UpdateRubberBand(e.GetPosition(CanvasRoot));
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isPanning && (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Left))
            {
                EndPan();
                e.Handled = true;
                return;
            }

            if (_isRubberBandSelecting && e.ChangedButton == MouseButton.Left)
            {
                EndRubberBand(e.GetPosition(CanvasRoot));
                this.ReleaseMouseCapture();
                e.Handled = true;
                return;
            }

            if (_isDraggingConnection)
            {
                var pos = e.GetPosition(CanvasRoot);
                var targetNode = FindInputPortAtPoint(pos);

                if (targetNode != null && targetNode != _connectionSourceNode)
                {
                    if (_connectionFromInput)
                    {
                        // Dragged from input port: target is source, source is the dropped-on node
                        ViewModel?.AddConnection(targetNode.Id, _connectionSourceNode!.Id);
                    }
                    else
                    {
                        ViewModel?.AddConnection(_connectionSourceNode!.Id, targetNode.Id);
                    }
                }

                EndConnectionDrag();
                this.ReleaseMouseCapture();
            }
        }
    }
}
