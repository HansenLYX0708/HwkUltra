using HWKUltra.Measurement.Abstractions;

namespace HWKUltra.Measurement.Implementations
{
    /// <summary>
    /// Configuration for a single measurement device instance.
    /// </summary>
    public class MeasurementConfig
    {
        /// <summary>
        /// Unique name for this measurement instance.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Device ID used by the native SDK (default 0).
        /// </summary>
        public int DeviceId { get; set; } = 0;

        /// <summary>
        /// Connection type: Usb or Ethernet.
        /// </summary>
        public MeasurementConnectionType ConnectionType { get; set; } = MeasurementConnectionType.Usb;

        /// <summary>
        /// Connection timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Default sampling cycle in microseconds (100, 200, 500, 1000). Default: 100.
        /// </summary>
        public int DefaultSamplingCycleUs { get; set; } = 100;

        /// <summary>
        /// Default moving average filter count. Default: 4.
        /// </summary>
        public int DefaultFilterAverage { get; set; } = 4;

        /// <summary>
        /// Max data buffer length for native API calls.
        /// </summary>
        public int MaxRequestDataLength { get; set; } = 512000;
    }
}
