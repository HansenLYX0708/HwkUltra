namespace HWKUltra.BarcodeScanner.Abstractions
{
    /// <summary>
    /// Barcode scanner connection status.
    /// </summary>
    public enum BarcodeScannerStatus
    {
        Disconnected = 0,
        Connected = 1,
        Error = 2
    }

    /// <summary>
    /// Event arguments raised when a barcode is received.
    /// </summary>
    public class BarcodeReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the scanner instance that received the barcode.
        /// </summary>
        public string InstanceName { get; }

        /// <summary>
        /// The decoded barcode string.
        /// </summary>
        public string Barcode { get; }

        /// <summary>
        /// Timestamp when the barcode was received.
        /// </summary>
        public DateTime Timestamp { get; }

        public BarcodeReceivedEventArgs(string instanceName, string barcode, DateTime timestamp)
        {
            InstanceName = instanceName;
            Barcode = barcode;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Barcode scanner status event arguments.
    /// </summary>
    public class BarcodeScannerStatusEventArgs : EventArgs
    {
        public string InstanceName { get; }
        public BarcodeScannerStatus Status { get; }

        public BarcodeScannerStatusEventArgs(string instanceName, BarcodeScannerStatus status)
        {
            InstanceName = instanceName;
            Status = status;
        }
    }
}
