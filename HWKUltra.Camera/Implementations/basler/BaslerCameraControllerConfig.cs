namespace HWKUltra.Camera.Implementations.basler
{
    /// <summary>
    /// Basler camera controller configuration (corresponds to ElmoMotionControllerConfig / CcsLightSourceControllerConfig).
    /// </summary>
    public class BaslerCameraControllerConfig
    {
        /// <summary>
        /// Preferred device type for camera enumeration: "USB", "CXP", or "Auto".
        /// Auto will try USB first, then CXP.
        /// </summary>
        public string DeviceType { get; set; } = "Auto";

        /// <summary>
        /// Maximum number of buffers for stream grabber.
        /// </summary>
        public int MaxBufferCount { get; set; } = 10;

        /// <summary>
        /// Camera definitions.
        /// </summary>
        public List<CameraConfig> Cameras { get; set; } = new();
    }
}
