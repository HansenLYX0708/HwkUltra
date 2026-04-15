namespace HWKUltra.BarcodeScanner.Abstractions
{
    /// <summary>
    /// Interface for barcode scanner controller operations.
    /// Manages multiple named scanner instances via serial port.
    /// </summary>
    public interface IBarcodeScannerController
    {
        /// <summary>
        /// Open the serial port for a scanner instance.
        /// </summary>
        void Open(string name);

        /// <summary>
        /// Close the serial port for a scanner instance.
        /// </summary>
        void Close(string name);

        /// <summary>
        /// Send a trigger command to initiate a scan.
        /// </summary>
        void Trigger(string name);

        /// <summary>
        /// Get the last received barcode for a scanner instance.
        /// </summary>
        string? GetLastBarcode(string name);

        /// <summary>
        /// Get the connection status of a scanner instance.
        /// </summary>
        BarcodeScannerStatus GetStatus(string name);

        /// <summary>
        /// Get all instance names.
        /// </summary>
        IReadOnlyList<string> InstanceNames { get; }

        /// <summary>
        /// Check if a named instance exists.
        /// </summary>
        bool HasInstance(string name);

        /// <summary>
        /// Fired when a barcode is received from any scanner.
        /// </summary>
        event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;

        /// <summary>
        /// Fired when scanner status changes.
        /// </summary>
        event EventHandler<BarcodeScannerStatusEventArgs>? StatusChanged;
    }
}
