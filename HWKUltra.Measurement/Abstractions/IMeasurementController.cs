namespace HWKUltra.Measurement.Abstractions
{
    /// <summary>
    /// Interface for measurement device controllers (e.g., laser displacement sensors).
    /// Supports multi-instance management, real-time measurement, trend/storage data acquisition,
    /// and device parameter configuration.
    /// </summary>
    public interface IMeasurementController
    {
        /// <summary>
        /// Open communication to the measurement device instance.
        /// </summary>
        void Open(string name);

        /// <summary>
        /// Close communication to the measurement device instance.
        /// </summary>
        void Close(string name);

        /// <summary>
        /// Get the current single-point measurement value (in mm).
        /// Returns -9999 if not connected or on error.
        /// </summary>
        double GetMeasurementValue(string name);

        /// <summary>
        /// Get the current trend data index for the specified instance.
        /// </summary>
        uint GetTrendIndex(string name);

        /// <summary>
        /// Get trend data within the specified index range.
        /// Returns an array of measurement values (in mm).
        /// </summary>
        double[] GetTrendData(string name, uint startIndex, uint endIndex);

        /// <summary>
        /// Get all trend data within the specified index range (raw, unfiltered).
        /// Returns an array of measurement values (in mm).
        /// </summary>
        double[] GetAllTrendData(string name, uint startIndex, uint endIndex);

        /// <summary>
        /// Get a single trend data point at the specified index (raw integer value).
        /// </summary>
        int GetTrendIndexData(string name, uint index);

        /// <summary>
        /// Start storage data collection on the specified instance.
        /// </summary>
        void StartStorage(string name);

        /// <summary>
        /// Stop storage data collection on the specified instance.
        /// </summary>
        void StopStorage(string name);

        /// <summary>
        /// Clear stored data on the specified instance.
        /// </summary>
        void ClearStorage(string name);

        /// <summary>
        /// Get the oldest storage index on the specified instance.
        /// </summary>
        uint GetStorageIndex(string name);

        /// <summary>
        /// Get storage data within the specified index range (raw integer values).
        /// </summary>
        int[] GetStorageData(string name, uint startIndex, uint endIndex);

        /// <summary>
        /// Enable or disable measurement on the specified instance.
        /// </summary>
        void MeasureControl(string name, bool enable);

        /// <summary>
        /// Set sampling cycle in microseconds (100, 200, 500, or 1000).
        /// </summary>
        void SetSamplingCycle(string name, int cycleUs);

        /// <summary>
        /// Set moving average filter count (1, 2, 4, 8, 16, 32, 64, 256, 1024, 4096, 16384, 65536, 262144).
        /// </summary>
        void SetFilterAverage(string name, int averageCount);

        /// <summary>
        /// Raised when the connection or measurement status of an instance changes.
        /// </summary>
        event EventHandler<MeasurementStatusEventArgs>? StatusChanged;
    }
}
