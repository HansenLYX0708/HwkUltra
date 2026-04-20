namespace HWKUltra.TestRun.Reports
{
    /// <summary>
    /// Summary statistics derived from a <see cref="TrayAoiReport"/>.
    /// </summary>
    public class DetectionSummary
    {
        public int TotalSliders { get; set; }
        public int DefectCount { get; set; }
        public int ErrorCount { get; set; }
        public int OffCount { get; set; }

        /// <summary>First-prime yield percentage in [0, 100].</summary>
        public double Yield => TotalSliders > 0
            ? (double)(TotalSliders - DefectCount - ErrorCount) / TotalSliders * 100.0
            : 0;
    }
}
