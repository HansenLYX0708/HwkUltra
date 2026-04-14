namespace HWKUltra.Camera.Abstractions
{
    /// <summary>
    /// Vendor-agnostic camera controller interface.
    /// Different camera vendors (Basler, Hikvision, etc.) implement this interface.
    /// </summary>
    public interface ICameraController
    {
        /// <summary>
        /// Open connection and initialize all configured cameras.
        /// </summary>
        void Open();

        /// <summary>
        /// Close all cameras and release resources.
        /// </summary>
        void Close();

        /// <summary>
        /// Grab a single frame from the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        /// <returns>True if grab was initiated successfully</returns>
        bool GrabOne(string cameraName);

        /// <summary>
        /// Start continuous grabbing on the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        /// <returns>True if continuous grab started successfully</returns>
        bool StartGrabbing(string cameraName);

        /// <summary>
        /// Stop continuous grabbing on the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        void StopGrabbing(string cameraName);

        /// <summary>
        /// Set exposure time for the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        /// <param name="value">Exposure time in microseconds</param>
        void SetExposureTime(string cameraName, long value);

        /// <summary>
        /// Get current exposure time for the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        /// <returns>Exposure time in microseconds</returns>
        long GetExposureTime(string cameraName);

        /// <summary>
        /// Set gain for the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        /// <param name="value">Gain value</param>
        void SetGain(string cameraName, long value);

        /// <summary>
        /// Get current gain for the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        /// <returns>Gain value</returns>
        long GetGain(string cameraName);

        /// <summary>
        /// Set trigger mode for the specified camera.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        /// <param name="mode">Trigger mode to set</param>
        void SetTriggerMode(string cameraName, CameraTriggerMode mode);

        /// <summary>
        /// Send a software trigger to the specified camera.
        /// Only valid when camera is in Software trigger mode.
        /// </summary>
        /// <param name="cameraName">Logical camera name</param>
        void SendSoftwareTrigger(string cameraName);

        /// <summary>
        /// Event raised when an image is grabbed from any camera.
        /// </summary>
        event EventHandler<CameraImageEventArgs> ImageGrabbed;
    }
}
