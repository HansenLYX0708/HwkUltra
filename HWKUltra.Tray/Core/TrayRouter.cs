using HWKUltra.Core;
using HWKUltra.Tray.Abstractions;

namespace HWKUltra.Tray.Core
{
    /// <summary>
    /// Routes tray operations to named instances via ITrayController.
    /// </summary>
    public class TrayRouter
    {
        private readonly ITrayController _controller;
        private readonly Dictionary<string, object> _instanceMap;

        public event EventHandler<TrayStatusEventArgs>? StatusChanged;

        public IReadOnlyList<string> InstanceNames => _controller.InstanceNames;
        public bool HasInstance(string name) => _controller.HasInstance(name);

        public TrayRouter(ITrayController controller, Dictionary<string, object> instanceMap)
        {
            _controller = controller;
            _instanceMap = instanceMap;
            _controller.StatusChanged += (s, e) => StatusChanged?.Invoke(this, e);
        }

        private void ValidateInstance(string name)
        {
            if (!_controller.HasInstance(name))
                throw new TrayException($"Tray instance '{name}' not found in router");
        }

        public void SetShape(string name, int rows, int cols)
        {
            ValidateInstance(name);
            _controller.SetShape(name, rows, cols);
        }

        public void InitPositions(string name, Point3D leftTop, Point3D rightTop, Point3D leftBottom, Point3D rightBottom)
        {
            ValidateInstance(name);
            _controller.InitPositions(name, leftTop, rightTop, leftBottom, rightBottom);
        }

        public Point3D GetPocketPosition(string name, int row, int col)
        {
            ValidateInstance(name);
            return _controller.GetPocketPosition(name, row, col);
        }

        public SlotState GetSlotState(string name, int row, int col)
        {
            ValidateInstance(name);
            return _controller.GetSlotState(name, row, col);
        }

        public void SetSlotState(string name, int row, int col, SlotState state)
        {
            ValidateInstance(name);
            _controller.SetSlotState(name, row, col, state);
        }

        public TrayTestState GetTestState(string name)
        {
            ValidateInstance(name);
            return _controller.GetTestState(name);
        }

        public void SetTestState(string name, TrayTestState state)
        {
            ValidateInstance(name);
            _controller.SetTestState(name, state);
        }

        public void ResetTray(string name)
        {
            ValidateInstance(name);
            _controller.ResetTray(name);
        }

        public TrayInfo GetTrayInfo(string name)
        {
            ValidateInstance(name);
            return _controller.GetTrayInfo(name);
        }

        public void SavePositions(string name, string filePath)
        {
            ValidateInstance(name);
            _controller.SavePositions(name, filePath);
        }

        public void LoadPositions(string name, string filePath)
        {
            ValidateInstance(name);
            _controller.LoadPositions(name, filePath);
        }
    }
}
