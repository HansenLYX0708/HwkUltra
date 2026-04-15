namespace HWKUltra.BarcodeScanner.Implementations
{
    /// <summary>
    /// Configuration for a single barcode scanner instance.
    /// </summary>
    public class BarcodeScannerConfig
    {
        /// <summary>
        /// Logical name for this scanner instance (e.g., "LeftScanner").
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Serial port name (e.g., "COM3").
        /// </summary>
        public string PortName { get; set; } = "";

        /// <summary>
        /// Baud rate (default 9600).
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// Data bits (default 8).
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// Parity: 0=None, 1=Odd, 2=Even, 3=Mark, 4=Space.
        /// </summary>
        public int Parity { get; set; } = 0;

        /// <summary>
        /// Stop bits: 0=None, 1=One, 2=Two, 3=OnePointFive.
        /// </summary>
        public int StopBits { get; set; } = 1;

        /// <summary>
        /// Optional trigger command string to send for scan initiation.
        /// Empty means no trigger command is needed.
        /// </summary>
        public string TriggerCommand { get; set; } = "";

        /// <summary>
        /// Read timeout in milliseconds (default 3000).
        /// </summary>
        public int ReadTimeoutMs { get; set; } = 3000;
    }
}
