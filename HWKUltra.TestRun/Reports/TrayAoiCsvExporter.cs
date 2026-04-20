namespace HWKUltra.TestRun.Reports
{
    /// <summary>
    /// Writes a <see cref="TrayAoiReport"/> as CSV in the exact format produced
    /// historically by HWKUltra.Tray's DetectionResult.SaveAsCsv — preserved verbatim
    /// so downstream systems parsing these files see no change.
    /// </summary>
    public static class TrayAoiCsvExporter
    {
        /// <summary>
        /// Persist the report to <paramref name="path"/>.
        /// </summary>
        /// <param name="report">The report to export.</param>
        /// <param name="path">Destination CSV path.</param>
        /// <param name="rotateCoordinates">If true, rotate defect coordinates 90 degrees (legacy compatibility).</param>
        public static void Save(TrayAoiReport report, string path, bool rotateCoordinates = false)
        {
            if (report is null) throw new ArgumentNullException(nameof(report));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path must be non-empty", nameof(path));
            if (string.IsNullOrEmpty(report.Session.SerialNumber))
                throw new InvalidOperationException("Cannot save CSV: SerialNumber is empty.");

            var summary = report.GetSummary();
            var yield = summary.TotalSliders > 0
                ? ((double)(summary.TotalSliders - summary.DefectCount - summary.ErrorCount)
                   / summary.TotalSliders * 100).ToString("F2") + "%"
                : "0%";

            var s = report.Session;
            var startStr = s.StartTime == default ? "" : s.StartTime.ToString("dd/MM/yyyy HH:mm:ss");
            var endStr = s.EndTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "";
            var durStr = s.EndTime.HasValue ? s.Duration.ToString() : "";

            using var sw = File.CreateText(path);

            sw.WriteLine($"Report timestamp: {DateTime.Now:dd/MM/yyyy HH:mm:ss}, test start timestamp: {startStr}, test finish timestamp: {endStr}, test duration: {durStr}.");
            sw.WriteLine($"LotID: {s.LotId},ProductName:{s.ProductName},HeadType:{s.HeadType},DeviceType:{s.DeviceType},ToolID:{s.ToolId}");
            sw.WriteLine($"trayID: {s.SerialNumber}");
            sw.WriteLine($"OperatorID: {s.OperatorId}");
            sw.WriteLine($"1st prime yield: {yield}");
            sw.WriteLine($"Total sliders: {summary.TotalSliders}");
            sw.WriteLine();

            sw.WriteLine("index,row,column,Slider Serial number,Defect category,Coordinate_1 X value,Coordinate_1 Y value,Coordinate_2 X value,Coordinate_2 Y value,Confidence,Default judge,Final judge");

            // Defect rows
            for (int i = 0; i < report.Defects.Count; i++)
            {
                var d = report.Defects[i];
                var sn = (d.Row >= 1 && d.Row <= report.Rows && d.Col >= 1 && d.Col <= report.Cols)
                    ? report.SliderSN[d.Row - 1, d.Col - 1] : "";
                var judge = (d.Row >= 1 && d.Row <= report.Rows && d.Col >= 1 && d.Col <= report.Cols)
                    ? report.SlotDefectCodes[d.Row - 1, d.Col - 1] : "";

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
            int idx = report.Defects.Count;
            for (int r = 0; r < report.Rows; r++)
                for (int c = 0; c < report.Cols; c++)
                {
                    var code = report.SlotDefectCodes[r, c];
                    if (string.IsNullOrEmpty(code) || code == "Pass" || code.Contains("error") || code == "OFF-T")
                    {
                        idx++;
                        sw.WriteLine($"{idx},{r + 1},{c + 1},{report.SliderSN[r, c]},,,,,,{code},");
                    }
                }
        }
    }
}
