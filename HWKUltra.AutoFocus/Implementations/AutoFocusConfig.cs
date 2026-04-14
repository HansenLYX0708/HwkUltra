namespace HWKUltra.AutoFocus.Implementations
{
    /// <summary>
    /// Per-instance auto focus configuration (corresponds to LightChannelConfig / CameraConfig).
    /// </summary>
    public class AutoFocusConfig
    {
        /// <summary>
        /// Logical instance name (e.g., "MainAF", "SubAF").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the LAF controller.
        /// </summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// TCP port of the LAF controller.
        /// Port 7777 = first configuration, 7778 = second configuration, etc.
        /// </summary>
        public int Port { get; set; } = 7777;

        /// <summary>
        /// Command timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 1000;
    }
}
