using HWKUltra.Measurement.Abstractions;
using HWKUltra.Measurement.Implementations;

namespace HWKUltra.Measurement.Core
{
    /// <summary>
    /// Routes measurement operations to named device instances.
    /// Provides name-based access and validates instance existence.
    /// </summary>
    public class MeasurementRouter
    {
        private readonly IMeasurementController _controller;
        private readonly Dictionary<string, MeasurementConfig> _instanceMap;

        public MeasurementRouter(
            IMeasurementController controller,
            Dictionary<string, MeasurementConfig> instanceMap)
        {
            _controller = controller;
            _instanceMap = instanceMap;
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

        public double GetMeasurementValue(string name)
        {
            ValidateInstance(name);
            return _controller.GetMeasurementValue(name);
        }

        public uint GetTrendIndex(string name)
        {
            ValidateInstance(name);
            return _controller.GetTrendIndex(name);
        }

        public double[] GetTrendData(string name, uint startIndex, uint endIndex)
        {
            ValidateInstance(name);
            return _controller.GetTrendData(name, startIndex, endIndex);
        }

        public double[] GetAllTrendData(string name, uint startIndex, uint endIndex)
        {
            ValidateInstance(name);
            return _controller.GetAllTrendData(name, startIndex, endIndex);
        }

        public int GetTrendIndexData(string name, uint index)
        {
            ValidateInstance(name);
            return _controller.GetTrendIndexData(name, index);
        }

        public void StartStorage(string name)
        {
            ValidateInstance(name);
            _controller.StartStorage(name);
        }

        public void StopStorage(string name)
        {
            ValidateInstance(name);
            _controller.StopStorage(name);
        }

        public void ClearStorage(string name)
        {
            ValidateInstance(name);
            _controller.ClearStorage(name);
        }

        public uint GetStorageIndex(string name)
        {
            ValidateInstance(name);
            return _controller.GetStorageIndex(name);
        }

        public int[] GetStorageData(string name, uint startIndex, uint endIndex)
        {
            ValidateInstance(name);
            return _controller.GetStorageData(name, startIndex, endIndex);
        }

        public void MeasureControl(string name, bool enable)
        {
            ValidateInstance(name);
            _controller.MeasureControl(name, enable);
        }

        public void SetSamplingCycle(string name, int cycleUs)
        {
            ValidateInstance(name);
            _controller.SetSamplingCycle(name, cycleUs);
        }

        public void SetFilterAverage(string name, int averageCount)
        {
            ValidateInstance(name);
            _controller.SetFilterAverage(name, averageCount);
        }

        public bool HasInstance(string name) => _instanceMap.ContainsKey(name);
        public IReadOnlyCollection<string> InstanceNames => _instanceMap.Keys;

        public event EventHandler<MeasurementStatusEventArgs>? StatusChanged
        {
            add => _controller.StatusChanged += value;
            remove => _controller.StatusChanged -= value;
        }

        private void ValidateInstance(string name)
        {
            if (!_instanceMap.ContainsKey(name))
                throw new ArgumentException($"Unknown measurement instance: {name}");
        }
    }
}
