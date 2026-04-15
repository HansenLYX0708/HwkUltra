using System.IO.Ports;
using System.Text;
using HWKUltra.BarcodeScanner.Abstractions;
using HWKUltra.Core;

namespace HWKUltra.BarcodeScanner.Implementations
{
    /// <summary>
    /// Serial port barcode scanner controller managing multiple named instances.
    /// Each instance has its own SerialPort and receive buffer (thread-safe).
    /// </summary>
    public class SerialBarcodeScannerController : IBarcodeScannerController
    {
        private readonly Dictionary<string, ScannerInstance> _instances = new();

        public event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;
        public event EventHandler<BarcodeScannerStatusEventArgs>? StatusChanged;

        public IReadOnlyList<string> InstanceNames => _instances.Keys.ToList();
        public bool HasInstance(string name) => _instances.ContainsKey(name);

        public SerialBarcodeScannerController(SerialBarcodeScannerControllerConfig config)
        {
            foreach (var cfg in config.Instances)
            {
                var instance = new ScannerInstance(cfg, OnBarcodeReceived, OnStatusChanged);
                _instances[cfg.Name] = instance;
            }
        }

        public void Open(string name)
        {
            var inst = GetInstance(name);
            inst.Open();
        }

        public void Close(string name)
        {
            var inst = GetInstance(name);
            inst.Close();
        }

        public void Trigger(string name)
        {
            var inst = GetInstance(name);
            inst.Trigger();
        }

        public string? GetLastBarcode(string name)
        {
            var inst = GetInstance(name);
            return inst.LastBarcode;
        }

        public BarcodeScannerStatus GetStatus(string name)
        {
            var inst = GetInstance(name);
            return inst.Status;
        }

        private ScannerInstance GetInstance(string name)
        {
            if (!_instances.TryGetValue(name, out var inst))
                throw new BarcodeScannerException($"Scanner instance '{name}' not found");
            return inst;
        }

        private void OnBarcodeReceived(string instanceName, string barcode)
        {
            BarcodeReceived?.Invoke(this, new BarcodeReceivedEventArgs(instanceName, barcode, DateTime.Now));
        }

        private void OnStatusChanged(string instanceName, BarcodeScannerStatus status)
        {
            StatusChanged?.Invoke(this, new BarcodeScannerStatusEventArgs(instanceName, status));
        }

        /// <summary>
        /// Internal class representing a single scanner instance with its own port and buffer.
        /// </summary>
        private class ScannerInstance
        {
            private readonly BarcodeScannerConfig _config;
            private readonly SerialPort _port;
            private readonly byte[] _buffer = new byte[1024];
            private int _bufferIndex;
            private readonly object _bufferLock = new();
            private readonly Action<string, string> _onBarcodeReceived;
            private readonly Action<string, BarcodeScannerStatus> _onStatusChanged;

            public string? LastBarcode { get; private set; }
            public BarcodeScannerStatus Status { get; private set; } = BarcodeScannerStatus.Disconnected;

            public ScannerInstance(
                BarcodeScannerConfig config,
                Action<string, string> onBarcodeReceived,
                Action<string, BarcodeScannerStatus> onStatusChanged)
            {
                _config = config;
                _onBarcodeReceived = onBarcodeReceived;
                _onStatusChanged = onStatusChanged;
                _port = new SerialPort();
            }

            public void Open()
            {
                try
                {
                    if (_port.IsOpen)
                        _port.Close();

                    _port.PortName = _config.PortName;
                    _port.BaudRate = _config.BaudRate;
                    _port.DataBits = _config.DataBits;
                    _port.Parity = (Parity)_config.Parity;
                    _port.StopBits = (StopBits)_config.StopBits;
                    _port.ReceivedBytesThreshold = 1;
                    _port.ReadTimeout = _config.ReadTimeoutMs;
                    _port.DataReceived += DataReceived;
                    _port.Open();

                    Status = BarcodeScannerStatus.Connected;
                    _onStatusChanged(_config.Name, Status);
                }
                catch (Exception ex)
                {
                    Status = BarcodeScannerStatus.Error;
                    _onStatusChanged(_config.Name, Status);
                    throw new BarcodeScannerException($"Failed to open scanner '{_config.Name}' on {_config.PortName}: {ex.Message}", ex);
                }
            }

            public void Close()
            {
                try
                {
                    if (_port.IsOpen)
                    {
                        _port.DataReceived -= DataReceived;
                        _port.Close();
                    }
                    Status = BarcodeScannerStatus.Disconnected;
                    _onStatusChanged(_config.Name, Status);
                }
                catch (Exception ex)
                {
                    Status = BarcodeScannerStatus.Error;
                    _onStatusChanged(_config.Name, Status);
                    throw new BarcodeScannerException($"Failed to close scanner '{_config.Name}': {ex.Message}", ex);
                }
            }

            public void Trigger()
            {
                if (!_port.IsOpen)
                    throw new BarcodeScannerException($"Scanner '{_config.Name}' is not open");

                if (!string.IsNullOrEmpty(_config.TriggerCommand))
                {
                    try
                    {
                        _port.Write(_config.TriggerCommand);
                    }
                    catch (Exception ex)
                    {
                        throw new BarcodeScannerException($"Failed to trigger scanner '{_config.Name}': {ex.Message}", ex);
                    }
                }
            }

            private void DataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                lock (_bufferLock)
                {
                    _bufferIndex = 0;
                    while (_port.BytesToRead > 0)
                    {
                        byte b = (byte)_port.ReadByte();
                        _buffer[_bufferIndex] = b;
                        _bufferIndex++;
                        if (_bufferIndex >= _buffer.Length)
                        {
                            _bufferIndex = 0;
                            _port.DiscardInBuffer();
                            return;
                        }
                    }

                    if (_bufferIndex > 0)
                    {
                        var barcode = Encoding.ASCII.GetString(_buffer, 0, _bufferIndex).Trim();
                        if (!string.IsNullOrEmpty(barcode))
                        {
                            LastBarcode = barcode;
                            _onBarcodeReceived(_config.Name, barcode);
                        }
                    }
                }
            }
        }
    }
}
