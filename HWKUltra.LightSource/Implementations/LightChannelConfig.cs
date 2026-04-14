namespace HWKUltra.LightSource.Implementations
{
    /// <summary>
    /// Light channel configuration (corresponds to AxisConfig in Motion).
    /// </summary>
    public class LightChannelConfig
    {
        /// <summary>
        /// Logical channel name (e.g., "TopLight", "BottomLight").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Physical channel index on the controller (0-based).
        /// </summary>
        public int ChannelIndex { get; set; }

        /// <summary>
        /// Default intensity value for this channel.
        /// </summary>
        public int DefaultIntensity { get; set; } = 512;

        /// <summary>
        /// Maximum intensity value for this channel.
        /// </summary>
        public int MaxIntensity { get; set; } = 1023;
    }
}
