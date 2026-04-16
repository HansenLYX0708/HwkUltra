using HWKUltra.AutoFocus.Abstractions;
using HWKUltra.AutoFocus.Implementations;
using HWKUltra.Core;

namespace HWKUltra.AutoFocus.Core
{
    /// <summary>
    /// Auto focus router - provides name-based access to AF instances
    /// (corresponds to MotionRouter / LightSourceRouter / CameraRouter).
    /// </summary>
    public class AutoFocusRouter
    {
        private readonly IAutoFocusController _controller;
        private readonly Dictionary<string, AutoFocusConfig> _instanceMap;

        public AutoFocusRouter(
            IAutoFocusController controller,
            Dictionary<string, AutoFocusConfig> instanceMap)
        {
            _controller = controller;
            _instanceMap = instanceMap;
        }

        public void Open() => _controller.Open();

        public void Close() => _controller.Close();

        /// <summary>
        /// Enable auto focus tracking on the specified instance.
        /// </summary>
        public void EnableAutoFocus(string name)
        {
            ValidateInstance(name);
            _controller.EnableAutoFocus(name);
        }

        /// <summary>
        /// Disable auto focus tracking on the specified instance.
        /// </summary>
        public void DisableAutoFocus(string name)
        {
            ValidateInstance(name);
            _controller.DisableAutoFocus(name);
        }

        /// <summary>
        /// Turn laser on for the specified instance.
        /// </summary>
        public void LaserOn(string name)
        {
            ValidateInstance(name);
            _controller.LaserOn(name);
        }

        /// <summary>
        /// Turn laser off for the specified instance.
        /// </summary>
        public void LaserOff(string name)
        {
            ValidateInstance(name);
            _controller.LaserOff(name);
        }

        /// <summary>
        /// Get current focus value from the specified instance.
        /// </summary>
        public double GetFocusValue(string name)
        {
            ValidateInstance(name);
            return _controller.GetFocusValue(name);
        }

        /// <summary>
        /// Get current motor position from the specified instance.
        /// </summary>
        public double GetMotorPosition(string name)
        {
            ValidateInstance(name);
            return _controller.GetMotorPosition(name);
        }

        /// <summary>
        /// Reset axis to zero position on the specified instance.
        /// </summary>
        public void ResetAxis(string name)
        {
            ValidateInstance(name);
            _controller.ResetAxis(name);
        }

        /// <summary>
        /// Send a custom command to the specified instance.
        /// </summary>
        public string SendCommand(string name, string command)
        {
            ValidateInstance(name);
            return _controller.SendCommand(name, command);
        }

        /// <summary>
        /// Check if an instance exists by name.
        /// </summary>
        public bool HasInstance(string name) => _instanceMap.ContainsKey(name);

        /// <summary>
        /// Get all instance names.
        /// </summary>
        public IReadOnlyCollection<string> InstanceNames => _instanceMap.Keys;

        /// <summary>
        /// StatusChanged event passthrough from the controller.
        /// </summary>
        public event EventHandler<AutoFocusStatusEventArgs>?  StatusChanged
        {
            add => _controller.StatusChanged += value;
            remove => _controller.StatusChanged -= value;
        }

        private void ValidateInstance(string name)
        {
            if (!_instanceMap.ContainsKey(name))
                throw new AutoFocusException(name,
                    $"Instance not found: {name}. Available: {string.Join(", ", _instanceMap.Keys)}");
        }
    }
}
