using HWKUltra.BarcodeScanner.Abstractions;
using HWKUltra.Core;

namespace HWKUltra.BarcodeScanner.Core
{
    /// <summary>
    /// Routes barcode scanner operations to named instances via IBarcodeScannerController.
    /// </summary>
    public class BarcodeScannerRouter
    {
        private readonly IBarcodeScannerController _controller;

        public event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;
        public event EventHandler<BarcodeScannerStatusEventArgs>? StatusChanged;

        public IReadOnlyList<string> InstanceNames => _controller.InstanceNames;
        public bool HasInstance(string name) => _controller.HasInstance(name);

        public BarcodeScannerRouter(IBarcodeScannerController controller)
        {
            _controller = controller;
            _controller.BarcodeReceived += (s, e) => BarcodeReceived?.Invoke(this, e);
            _controller.StatusChanged += (s, e) => StatusChanged?.Invoke(this, e);
        }

        private void ValidateInstance(string name)
        {
            if (!_controller.HasInstance(name))
                throw new BarcodeScannerException($"Scanner instance '{name}' not found in router");
        }

        public void Open(string name)
        {
            ValidateInstance(name);
            _controller.Open(name);
        }

        public void Close(string name)
        {
            ValidateInstance(name);
            _controller.Close(name);
        }

        public void Trigger(string name)
        {
            ValidateInstance(name);
            _controller.Trigger(name);
        }

        public string? GetLastBarcode(string name)
        {
            ValidateInstance(name);
            return _controller.GetLastBarcode(name);
        }

        public BarcodeScannerStatus GetStatus(string name)
        {
            ValidateInstance(name);
            return _controller.GetStatus(name);
        }
    }
}
