namespace HWKUltra.Tray.Implementations.WDTray
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

    /// <summary>
    /// Definition of a slot state (e.g., Empty, Present, Pass, Fail).
    /// Loaded from JSON config to avoid hardcoded enums.
    /// </summary>
    public class SlotStateDefinition
    {
        /// <summary>
        /// State code (e.g., "Empty", "Present", "Pass", "Fail", "Error").
        /// </summary>
        public string Code { get; set; } = "";

        /// <summary>
        /// Category: "State" for slot lifecycle states.
        /// </summary>
        public string Category { get; set; } = "State";

        /// <summary>
        /// Human-readable description.
        /// </summary>
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Definition of a defect code (e.g., A2, A5, P0532).
    /// Loaded from JSON config — different products may define different defect codes.
    /// </summary>
    public class DefectCodeDefinition
    {
        /// <summary>
        /// Defect code string (e.g., "A2", "A5", "P0532").
        /// </summary>
        public string Code { get; set; } = "";

        /// <summary>
        /// Category: "Defect" for inspection defects.
        /// </summary>
        public string Category { get; set; } = "Defect";

        /// <summary>
        /// Human-readable description of this defect type.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Severity level (e.g., "Major", "Minor", "Critical").
        /// </summary>
        public string Severity { get; set; } = "";
    }
}
