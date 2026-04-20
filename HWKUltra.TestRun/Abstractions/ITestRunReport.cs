using HWKUltra.Core;

namespace HWKUltra.TestRun.Abstractions
{
    /// <summary>
    /// Marker interface every station-specific report implements.
    /// Guarantees access to the universal <see cref="TestSession"/> metadata.
    /// Station-specific data (slot grids, defects, measurements, barcodes, ...)
    /// lives on the concrete implementation.
    /// </summary>
    public interface ITestRunReport
    {
        /// <summary>
        /// Universal session metadata (lot, operator, product, times, etc.).
        /// </summary>
        TestSession Session { get; }
    }
}
