namespace HWKUltra.Tray.Abstractions
{
    /// <summary>
    /// Tray test state enumeration.
    /// </summary>
    public enum TrayTestState
    {
        Idle = 0,
        Testing = 1,
        Complete = 2,
        Error = 3
    }

    /// <summary>
    /// Slot state enumeration for individual pocket in a tray.
    /// </summary>
    public enum SlotState
    {
        Empty = 0,
        Present = 1,
        Pass = 2,
        Fail = 3,
        Error = 4,
        Unknown = 5
    }

    /// <summary>
    /// Tray statistical information.
    /// </summary>
    public class TrayInfo
    {
        public string Name { get; set; } = "";
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int TotalSlots => Rows * Cols;
        public int TestedCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int ErrorCount { get; set; }
        public TrayTestState TestState { get; set; } = TrayTestState.Idle;
    }

    /// <summary>
    /// Tray status event arguments.
    /// </summary>
    public class TrayStatusEventArgs : EventArgs
    {
        public string InstanceName { get; }
        public TrayInfo Info { get; }

        public TrayStatusEventArgs(string instanceName, TrayInfo info)
        {
            InstanceName = instanceName;
            Info = info;
        }
    }

    /// <summary>
    /// Bounding box coordinates for a defect region.
    /// </summary>
    public record BoundingBox(int X1, int Y1, int X2, int Y2);

    /// <summary>
    /// Detail of a single defect found during inspection.
    /// DefectCode is a string (loaded from JSON config, not a hardcoded enum).
    /// </summary>
    public class DefectDetail
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string DefectCode { get; set; } = "";
        public BoundingBox? Region { get; set; }
        public float Confidence { get; set; }
        public int ImgRows { get; set; }
        public int ImgCols { get; set; }
    }

    /// <summary>
    /// Summary statistics for a detection result.
    /// </summary>
    public class DetectionSummary
    {
        public int TotalSliders { get; set; }
        public int DefectCount { get; set; }
        public int ErrorCount { get; set; }
        public int OffCount { get; set; }
        public double Yield => TotalSliders > 0
            ? (double)(TotalSliders - DefectCount - ErrorCount) / TotalSliders * 100.0
            : 0;
    }

    /// <summary>
    /// Detection result for a tray, replacing the old TrayDetectionResult class.
    /// Uses string-based defect codes instead of hardcoded enums.
    /// </summary>
    public class DetectionResult
    {
        public string TrayName { get; set; } = "";
        public int Rows { get; set; }
        public int Cols { get; set; }
        public string SerialNum { get; set; } = "";
        public string LotId { get; set; } = "";
        public string LoadLock { get; set; } = "";
        public string OperationId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string HeadType { get; set; } = "";
        public string ToolId { get; set; } = "";
        public string StartTestTime { get; set; } = "";
        public string EndTestTime { get; set; } = "";
        public string TestDuration { get; set; } = "";
        public string CsvOutputPath { get; set; } = "";

        /// <summary>
        /// Slider serial numbers grid [row, col].
        /// </summary>
        public string[,] SliderSN { get; private set; }

        /// <summary>
        /// Container IDs grid [row, col].
        /// </summary>
        public string[,] ContainerIds { get; private set; }

        /// <summary>
        /// Slot defect codes grid [row, col] — string-based (e.g., "A2", "Pass", "").
        /// </summary>
        public string[,] SlotDefectCodes { get; private set; }

        /// <summary>
        /// Detailed defect list from inspection.
        /// </summary>
        public List<DefectDetail> Defects { get; set; } = new();

        public DetectionResult(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            SliderSN = new string[rows, cols];
            ContainerIds = new string[rows, cols];
            SlotDefectCodes = new string[rows, cols];
            Initialize();
        }

        /// <summary>
        /// Reset all grid data.
        /// </summary>
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

        /// <summary>
        /// Compute summary statistics.
        /// </summary>
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

        /// <summary>
        /// Save detection result as CSV (preserves original CSV format).
        /// </summary>
        public void SaveAsCsv(string path, bool rotateCoordinates = false)
        {
            if (string.IsNullOrEmpty(SerialNum))
                throw new InvalidOperationException("Cannot save CSV: SerialNum is empty.");

            var summary = GetSummary();
            var yield = summary.TotalSliders > 0
                ? ((double)(summary.TotalSliders - summary.DefectCount - summary.ErrorCount) / summary.TotalSliders * 100).ToString("F2") + "%"
                : "0%";

            using var sw = File.CreateText(path);

            sw.WriteLine($"Report timestamp: {DateTime.Now:dd/MM/yyyy HH:mm:ss}, test start timestamp: {StartTestTime}, test finish timestamp: {EndTestTime}, test duration: {TestDuration}.");
            sw.WriteLine($"LotID: {LotId},ProductName:{ProductName},HeadType:{HeadType},DeviceType:{DeviceType},ToolID:{ToolId}");
            sw.WriteLine($"trayID: {SerialNum}");
            sw.WriteLine($"OperatorID: {OperationId}");
            sw.WriteLine($"1st prime yield: {yield}");
            sw.WriteLine($"Total sliders: {summary.TotalSliders}");
            sw.WriteLine();

            sw.WriteLine("index,row,column,Slider Serial number,Defect category,Coordinate_1 X value,Coordinate_1 Y value,Coordinate_2 X value,Coordinate_2 Y value,Confidence,Default judge,Final judge");

            // Defect rows
            for (int i = 0; i < Defects.Count; i++)
            {
                var d = Defects[i];
                var sn = (d.Row >= 1 && d.Row <= Rows && d.Col >= 1 && d.Col <= Cols)
                    ? SliderSN[d.Row - 1, d.Col - 1] : "";
                var judge = (d.Row >= 1 && d.Row <= Rows && d.Col >= 1 && d.Col <= Cols)
                    ? SlotDefectCodes[d.Row - 1, d.Col - 1] : "";

                int x1, y1, x2, y2;
                if (rotateCoordinates && d.Region != null)
                {
                    x1 = d.ImgRows - d.Region.Y2;
                    y1 = d.Region.X1;
                    x2 = d.ImgRows - d.Region.Y1;
                    y2 = d.Region.X2;
                }
                else if (d.Region != null)
                {
                    x1 = d.Region.X1; y1 = d.Region.Y1;
                    x2 = d.Region.X2; y2 = d.Region.Y2;
                }
                else
                {
                    x1 = y1 = x2 = y2 = 0;
                }

                sw.WriteLine($"{i + 1},{d.Row},{d.Col},{sn},{d.DefectCode},{x1},{y1},{x2},{y2},{d.Confidence},{judge},");
            }

            // Non-defect rows
            int idx = Defects.Count;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    var code = SlotDefectCodes[r, c];
                    if (string.IsNullOrEmpty(code) || code == "Pass" || code.Contains("error") || code == "OFF-T")
                    {
                        idx++;
                        sw.WriteLine($"{idx},{r + 1},{c + 1},{SliderSN[r, c]},,,,,,{code},");
                    }
                }
        }
    }
}
