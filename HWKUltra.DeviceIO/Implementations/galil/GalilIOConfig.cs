namespace HWKUltra.DeviceIO.Implementations.galil
{
    /// <summary>
    /// Galil IO controller configuration (corresponds to ElmoMotionControllerConfig).
    /// </summary>
    public class GalilIOConfig
    {
        /// <summary>
        /// Galil card configuration list (supports multiple cards).
        /// </summary>
        public List<GalilCardConfig> Cards { get; set; } = new();

        /// <summary>
        /// Input point configuration list.
        /// </summary>
        public List<IOPointConfig> Inputs { get; set; } = new();

        /// <summary>
        /// Output point configuration list.
        /// </summary>
        public List<IOPointConfig> Outputs { get; set; } = new();

        /// <summary>
        /// IO status monitoring poll interval (ms).
        /// </summary>
        public int MonitorIntervalMs { get; set; } = 100;

        /// <summary>
        /// Output point names to turn on by default after connection.
        /// </summary>
        public List<string> DefaultOnOutputs { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a single Galil card.
    /// </summary>
    public class GalilCardConfig
    {
        /// <summary>
        /// Card index (0, 1, ...)
        /// </summary>
        public int CardIndex { get; set; }

        /// <summary>
        /// IP address (with connection params, e.g. "192.168.1.101 -d")
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;
    }
}
