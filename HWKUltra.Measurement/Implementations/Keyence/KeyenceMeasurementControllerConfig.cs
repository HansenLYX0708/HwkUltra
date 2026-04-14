namespace HWKUltra.Measurement.Implementations.Keyence
{
    /// <summary>
    /// Configuration for the Keyence CL3-IF measurement controller.
    /// Supports multiple device instances.
    /// </summary>
    public class KeyenceMeasurementControllerConfig
    {
        /// <summary>
        /// List of measurement device instances.
        /// </summary>
        public List<MeasurementConfig> Instances { get; set; } = new();
    }
}
