namespace HWKUltra.Core
{
    /// <summary>
    /// Universal test-session metadata. Applies to any station type
    /// (AOI+Tray, barcode-only, measurement-only, etc.).
    /// Captures identity (lot, operator, product, tool) and timing.
    /// Device-domain data (slot grids, defects, measurements) lives in
    /// station-specific report types that reference this session.
    /// </summary>
    public class TestSession
    {
        /// <summary>Unique ID for this session (defaults to a new GUID).</summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>MES lot identifier.</summary>
        public string LotId { get; set; } = "";

        /// <summary>Operator / user identifier who started the run.</summary>
        public string OperatorId { get; set; } = "";

        /// <summary>Product family name.</summary>
        public string ProductName { get; set; } = "";

        /// <summary>Product device type / variant.</summary>
        public string DeviceType { get; set; } = "";

        /// <summary>Head type (tooling head used for this run).</summary>
        public string HeadType { get; set; } = "";

        /// <summary>Tool / station identifier.</summary>
        public string ToolId { get; set; } = "";

        /// <summary>
        /// Serial number of the unit under test for this run
        /// (for tray stations this is the tray serial / ID).
        /// </summary>
        public string SerialNumber { get; set; } = "";

        /// <summary>Run start timestamp (set when the run is created).</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Run end timestamp; null while the run is still active.</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>Effective duration (live if run not complete yet).</summary>
        public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
    }
}
