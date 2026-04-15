namespace HWKUltra.BarcodeScanner.Implementations
{
    /// <summary>
    /// Configuration for serial barcode scanner controller, holding multiple scanner instances.
    /// </summary>
    public class SerialBarcodeScannerControllerConfig
    {
        /// <summary>
        /// List of scanner instance configurations.
        /// </summary>
        public List<BarcodeScannerConfig> Instances { get; set; } = new();
    }
}
