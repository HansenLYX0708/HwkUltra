namespace HWKUltra.Tray.Implementations.WDTray
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

        /// <summary>
        /// Slot state definitions (e.g., Empty, Present, Pass, Fail).
        /// If empty or null, default built-in states are used.
        /// </summary>
        public List<SlotStateDefinition>? SlotStates { get; set; }

        /// <summary>
        /// Defect code definitions (e.g., A2, A5, P0532).
        /// If empty or null, no defect codes are pre-defined.
        /// </summary>
        public List<DefectCodeDefinition>? DefectCodes { get; set; }
    }
}
