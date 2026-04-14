namespace HWKUltra.Tray.Implementations
{
    /// <summary>
    /// Configuration for a single tray instance.
    /// </summary>
    public class TrayConfig
    {
        /// <summary>
        /// Logical name for this tray instance (e.g., "Tray1").
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Number of rows in this tray.
        /// </summary>
        public int Rows { get; set; } = 8;

        /// <summary>
        /// Number of columns in this tray.
        /// </summary>
        public int Cols { get; set; } = 30;

        /// <summary>
        /// Optional path to save/load pocket position data (JSON).
        /// </summary>
        public string PositionDataPath { get; set; } = "";
    }
}
