namespace HWKUltra.DeviceIO.Implementations
{
    /// <summary>
    /// IO point configuration (used for both inputs and outputs).
    /// </summary>
    public class IOPointConfig
    {
        /// <summary>
        /// IO point name, e.g. "LeftTrayBaseVacuum1"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Card index (0-based), corresponds to GalilCardConfig.CardIndex
        /// </summary>
        public int CardIndex { get; set; }

        /// <summary>
        /// Bank number (0, 1, ...)
        /// </summary>
        public int BankIndex { get; set; }

        /// <summary>
        /// Bit index within the bank
        /// </summary>
        public int BitIndex { get; set; }

        /// <summary>
        /// Optional description
        /// </summary>
        public string? Description { get; set; }
    }
}
