using HWKUltra.DeviceIO.Abstractions;
using HWKUltra.DeviceIO.Implementations;

namespace HWKUltra.DeviceIO.Core
{
    /// <summary>
    /// IO router (corresponds to MotionRouter).
    /// Access IO points by string name, hiding low-level card/bit details.
    /// </summary>
    public class IORouter
    {
        private readonly IIOController _controller;
        private readonly Dictionary<string, IOPointConfig> _inputMap;
        private readonly Dictionary<string, IOPointConfig> _outputMap;
        private readonly Dictionary<(int cardIndex, int bankIndex, int bitIndex), string> _inputReverseMap;
        private readonly Dictionary<(int cardIndex, int bankIndex, int bitIndex), string> _outputReverseMap;

        private Timer? _monitorTimer;
        private readonly int _monitorIntervalMs;
        private readonly object _monitorLock = new();
        private bool _monitorRunning;

        /// <summary>
        /// IO status changed event.
        /// </summary>
        public event Action<Dictionary<string, bool>, Dictionary<string, bool>>? IOStatusChanged;

        public IORouter(
            IIOController controller,
            List<IOPointConfig> inputs,
            List<IOPointConfig> outputs,
            int monitorIntervalMs = 100)
        {
            _controller = controller;
            _monitorIntervalMs = monitorIntervalMs;

            _inputMap = inputs.ToDictionary(p => p.Name, p => p);
            _outputMap = outputs.ToDictionary(p => p.Name, p => p);

            _inputReverseMap = inputs.ToDictionary(
                p => (p.CardIndex, p.BankIndex, p.BitIndex),
                p => p.Name);
            _outputReverseMap = outputs.ToDictionary(
                p => (p.CardIndex, p.BankIndex, p.BitIndex),
                p => p.Name);
        }

        /// <summary>
        /// Set output by name.
        /// </summary>
        public void SetOutput(string name, bool value)
        {
            var point = GetOutputPoint(name);
            _controller.SetOutput(point.CardIndex, point.BankIndex * 8 + point.BitIndex, value);
        }

        /// <summary>
        /// Read output state by name.
        /// </summary>
        public bool GetOutput(string name)
        {
            var point = GetOutputPoint(name);
            return _controller.GetOutput(point.CardIndex, point.BankIndex * 8 + point.BitIndex);
        }

        /// <summary>
        /// Read input state by name.
        /// </summary>
        public bool GetInput(string name)
        {
            var point = GetInputPoint(name);
            return _controller.GetInput(point.CardIndex, point.BankIndex * 8 + point.BitIndex);
        }

        /// <summary>
        /// Batch-read all input states.
        /// </summary>
        public Dictionary<string, bool> ReadAllInputs()
        {
            var result = new Dictionary<string, bool>();
            // Group by card+bank to minimize communication calls
            var groups = _inputMap.Values
                .GroupBy(p => (p.CardIndex, p.BankIndex));

            foreach (var group in groups)
            {
                int bankValue = _controller.ReadInputBank(group.Key.CardIndex, group.Key.BankIndex);
                foreach (var point in group)
                {
                    result[point.Name] = ((bankValue >> point.BitIndex) & 1) != 0;
                }
            }
            return result;
        }

        /// <summary>
        /// Batch-read all output states.
        /// </summary>
        public Dictionary<string, bool> ReadAllOutputs()
        {
            var result = new Dictionary<string, bool>();
            var groups = _outputMap.Values
                .GroupBy(p => (p.CardIndex, p.BankIndex));

            foreach (var group in groups)
            {
                int bankValue = _controller.ReadOutputBank(group.Key.CardIndex, group.Key.BankIndex);
                foreach (var point in group)
                {
                    result[point.Name] = ((bankValue >> point.BitIndex) & 1) != 0;
                }
            }
            return result;
        }

        /// <summary>
        /// Start IO status monitoring.
        /// </summary>
        public void StartMonitor()
        {
            if (_monitorRunning) return;
            _monitorRunning = true;
            _monitorTimer = new Timer(MonitorCallback, null, _monitorIntervalMs, Timeout.Infinite);
        }

        /// <summary>
        /// Stop IO status monitoring.
        /// </summary>
        public void StopMonitor()
        {
            _monitorRunning = false;
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }

        /// <summary>
        /// Check if an input point exists.
        /// </summary>
        public bool HasInput(string name) => _inputMap.ContainsKey(name);

        /// <summary>
        /// Check if an output point exists.
        /// </summary>
        public bool HasOutput(string name) => _outputMap.ContainsKey(name);

        /// <summary>
        /// Get all input point names.
        /// </summary>
        public IReadOnlyCollection<string> InputNames => _inputMap.Keys;

        /// <summary>
        /// Get all output point names.
        /// </summary>
        public IReadOnlyCollection<string> OutputNames => _outputMap.Keys;

        private void MonitorCallback(object? state)
        {
            if (!_monitorRunning) return;

            try
            {
                lock (_monitorLock)
                {
                    var inputs = ReadAllInputs();
                    var outputs = ReadAllOutputs();
                    IOStatusChanged?.Invoke(inputs, outputs);
                }
            }
            catch
            {
                // Monitor exceptions should not interrupt polling
            }
            finally
            {
                if (_monitorRunning)
                {
                    _monitorTimer?.Change(_monitorIntervalMs, Timeout.Infinite);
                }
            }
        }

        private IOPointConfig GetInputPoint(string name)
        {
            if (!_inputMap.TryGetValue(name, out var point))
                throw new KeyNotFoundException($"Input IO point not found: {name}. Available: {string.Join(", ", _inputMap.Keys)}");
            return point;
        }

        private IOPointConfig GetOutputPoint(string name)
        {
            if (!_outputMap.TryGetValue(name, out var point))
                throw new KeyNotFoundException($"Output IO point not found: {name}. Available: {string.Join(", ", _outputMap.Keys)}");
            return point;
        }
    }
}
