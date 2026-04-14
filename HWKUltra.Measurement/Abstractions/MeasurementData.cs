namespace HWKUltra.Measurement.Abstractions
{
    /// <summary>
    /// Connection type for the measurement device.
    /// </summary>
    public enum MeasurementConnectionType
    {
        Usb = 0,
        Ethernet = 1
    }

    /// <summary>
    /// Status of a measurement device instance.
    /// </summary>
    public class MeasurementStatus
    {
        /// <summary>
        /// Whether the device is currently connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// The connection type (USB or Ethernet).
        /// </summary>
        public MeasurementConnectionType ConnectionType { get; set; }

        /// <summary>
        /// Last measured value (in mm). -9999 indicates no valid reading.
        /// </summary>
        public double LastValue { get; set; } = -9999;
    }

    /// <summary>
    /// Event args raised when measurement device status changes.
    /// </summary>
    public class MeasurementStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Instance name that raised the event.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Current status snapshot.
        /// </summary>
        public MeasurementStatus Status { get; }

        /// <summary>
        /// Timestamp of the event.
        /// </summary>
        public DateTime Timestamp { get; }

        public MeasurementStatusEventArgs(string name, MeasurementStatus status)
        {
            Name = name;
            Status = status;
            Timestamp = DateTime.Now;
        }
    }
}
