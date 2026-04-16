using HWKUltra.Camera.Abstractions;
using HWKUltra.Camera.Implementations;
using HWKUltra.Core;

namespace HWKUltra.Camera.Core
{
    /// <summary>
    /// Camera router - provides name-based access to cameras (corresponds to MotionRouter / LightSourceRouter).
    /// Delegates operations to the underlying ICameraController by camera name.
    /// </summary>
    public class CameraRouter
    {
        private readonly ICameraController _controller;
        private readonly Dictionary<string, CameraConfig> _cameraMap;

        /// <summary>
        /// Event raised when an image is grabbed. Routes from the underlying controller.
        /// </summary>
        public event EventHandler<CameraImageEventArgs>? ImageGrabbed;

        public CameraRouter(
            ICameraController controller,
            Dictionary<string, CameraConfig> cameraMap)
        {
            _controller = controller;
            _cameraMap = cameraMap;

            // Forward image events from controller
            _controller.ImageGrabbed += (sender, e) => ImageGrabbed?.Invoke(this, e);
        }

        public void Open() => _controller.Open();

        public void Close() => _controller.Close();

        /// <summary>
        /// Grab a single frame from the specified camera.
        /// </summary>
        public bool GrabOne(string cameraName)
        {
            ValidateCamera(cameraName);
            return _controller.GrabOne(cameraName);
        }

        /// <summary>
        /// Start continuous grabbing on the specified camera.
        /// </summary>
        public bool StartGrabbing(string cameraName)
        {
            ValidateCamera(cameraName);
            return _controller.StartGrabbing(cameraName);
        }

        /// <summary>
        /// Stop continuous grabbing on the specified camera.
        /// </summary>
        public void StopGrabbing(string cameraName)
        {
            ValidateCamera(cameraName);
            _controller.StopGrabbing(cameraName);
        }

        /// <summary>
        /// Set exposure time for the specified camera.
        /// </summary>
        public void SetExposureTime(string cameraName, long value)
        {
            ValidateCamera(cameraName);
            _controller.SetExposureTime(cameraName, value);
        }

        /// <summary>
        /// Get current exposure time for the specified camera.
        /// </summary>
        public long GetExposureTime(string cameraName)
        {
            ValidateCamera(cameraName);
            return _controller.GetExposureTime(cameraName);
        }

        /// <summary>
        /// Set gain for the specified camera.
        /// </summary>
        public void SetGain(string cameraName, long value)
        {
            ValidateCamera(cameraName);
            _controller.SetGain(cameraName, value);
        }

        /// <summary>
        /// Get current gain for the specified camera.
        /// </summary>
        public long GetGain(string cameraName)
        {
            ValidateCamera(cameraName);
            return _controller.GetGain(cameraName);
        }

        /// <summary>
        /// Set trigger mode for the specified camera.
        /// </summary>
        public void SetTriggerMode(string cameraName, CameraTriggerMode mode)
        {
            ValidateCamera(cameraName);
            _controller.SetTriggerMode(cameraName, mode);
        }

        /// <summary>
        /// Send a software trigger to the specified camera.
        /// </summary>
        public void SendSoftwareTrigger(string cameraName)
        {
            ValidateCamera(cameraName);
            _controller.SendSoftwareTrigger(cameraName);
        }

        /// <summary>
        /// Check if a camera exists by name.
        /// </summary>
        public bool HasCamera(string cameraName) => _cameraMap.ContainsKey(cameraName);

        /// <summary>
        /// Get all configured camera names.
        /// </summary>
        public IReadOnlyCollection<string> CameraNames => _cameraMap.Keys;

        private void ValidateCamera(string cameraName)
        {
            if (!_cameraMap.TryGetValue(cameraName, out _))
                throw new CameraException(cameraName,
                    $"Camera not found: {cameraName}. Available: [{string.Join(", ", _cameraMap.Keys)}]");
        }
    }
}
