namespace HWKUltra.Camera.Implementations
{
    /// <summary>
    /// Per-camera configuration (corresponds to LightChannelConfig / AxisConfig).
    /// </summary>
    public class CameraConfig
    {
        /// <summary>
        /// Logical camera name (e.g., "DetectCam", "AlignCam").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Physical camera serial number.
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Image width in pixels.
        /// </summary>
        public int Width { get; set; } = 2048;

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        public int Height { get; set; } = 2048;

        /// <summary>
        /// Default exposure time in microseconds.
        /// </summary>
        public long DefaultExposure { get; set; } = 50;

        /// <summary>
        /// Default gain value.
        /// </summary>
        public long DefaultGain { get; set; } = 0;

        /// <summary>
        /// Offset mode: 0 = TopLeft (OffsetX=0, OffsetY=0), 1 = Center.
        /// </summary>
        public int OffsetMode { get; set; } = 0;
    }
}
