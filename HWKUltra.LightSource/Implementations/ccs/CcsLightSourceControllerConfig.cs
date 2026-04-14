namespace HWKUltra.LightSource.Implementations.ccs
{
    /// <summary>
    /// CCS light source controller configuration (corresponds to ElmoMotionControllerConfig).
    /// </summary>
    public class CcsLightSourceControllerConfig
    {
        /// <summary>
        /// IP address of the CCS light controller.
        /// </summary>
        public string IpAddress { get; set; } = "192.168.1.80";

        /// <summary>
        /// TCP port of the CCS light controller.
        /// </summary>
        public int Port { get; set; } = 40001;

        /// <summary>
        /// Connection timeout in milliseconds.
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 3000;

        /// <summary>
        /// Delay between sequential commands in milliseconds.
        /// </summary>
        public int CommandDelayMs { get; set; } = 100;

        /// <summary>
        /// Light channel definitions.
        /// </summary>
        public List<LightChannelConfig> Channels { get; set; } = new();
    }
}
