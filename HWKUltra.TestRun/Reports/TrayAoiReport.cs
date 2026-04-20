using HWKUltra.Core;
using HWKUltra.TestRun.Abstractions;

namespace HWKUltra.TestRun.Reports
{
    /// <summary>
    /// AOI+Tray station inspection report (formerly HWKUltra.Tray's DetectionResult).
    /// Holds the live, in-memory aggregate for a single tray run:
    /// session metadata + slot grid + accumulated defects.
    /// Implements <see cref="ITestRunReport"/> so it can be stored and observed
    /// through the cross-cutting TestRun session layer.
    /// </summary>
    public class TrayAoiReport : ITestRunReport
    {
        /// <inheritdoc />
        public TestSession Session { get; } = new();

        // --- Tray-station-specific fields ---

        public string TrayName { get; set; } = "";
        public int Rows { get; }
        public int Cols { get; }

        /// <summary>Optional path where CSV will be persisted at run end.</summary>
        public string CsvOutputPath { get; set; } = "";

        /// <summary>Load-lock identifier.</summary>
        public string LoadLock { get; set; } = "";

        /// <summary>Slider serial numbers grid [row, col].</summary>
        public string[,] SliderSN { get; }

        /// <summary>Container IDs grid [row, col].</summary>
        public string[,] ContainerIds { get; }

        /// <summary>Slot defect codes grid [row, col] — string-based (e.g., "A2", "Pass", "").</summary>
        public string[,] SlotDefectCodes { get; }

        /// <summary>Detailed defect list accumulated during inspection.</summary>
        public List<DefectDetail> Defects { get; } = new();

        public TrayAoiReport(int rows, int cols)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));
            Rows = rows;
            Cols = cols;
            SliderSN = new string[rows, cols];
            ContainerIds = new string[rows, cols];
            SlotDefectCodes = new string[rows, cols];
            Initialize();
        }

        /// <summary>Reset all grid data and clear defect list.</summary>
        public void Initialize()
        {
            LoadLock = string.Empty;
            Defects.Clear();
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    SliderSN[i, j] = string.Empty;
                    ContainerIds[i, j] = string.Empty;
                    SlotDefectCodes[i, j] = string.Empty;
                }
        }

        /// <summary>Compute summary statistics.</summary>
        public DetectionSummary GetSummary()
        {
            var summary = new DetectionSummary();
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    var code = SlotDefectCodes[i, j];
                    if (!string.IsNullOrEmpty(code) && code != "Empty")
                        summary.TotalSliders++;
                    if (!string.IsNullOrEmpty(code) && code.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        summary.ErrorCount++;
                    if (code == "OFF" || code == "OFF-T")
                        summary.OffCount++;
                }
            summary.DefectCount = Defects.Count;
            return summary;
        }
    }
}
