namespace HWKUltra.Tray.Implementations
{
    /// <summary>
    /// Configuration for the tray controller, holding multiple tray instances.
    /// </summary>
    public class TrayControllerConfig
    {
        /// <summary>
        /// List of tray instance configurations.
        /// </summary>
        public List<TrayConfig> Instances { get; set; } = new();
    }
}
