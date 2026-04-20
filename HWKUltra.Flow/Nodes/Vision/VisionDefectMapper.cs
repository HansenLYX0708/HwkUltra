using HWKUltra.TestRun.Reports;
using HWKUltra.Vision.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Maps a pure vision detection (category + confidence + bbox) into a
    /// slot-enriched <see cref="DefectDetail"/> suitable for appending to
    /// a <see cref="TrayAoiReport"/>.
    /// </summary>
    public static class VisionDefectMapper
    {
        public static DefectDetail ToDefectDetail(
            VisionDetection v,
            int row,
            int col,
            int imgRows,
            int imgCols,
            string? defectCodeOverride = null)
            => new DefectDetail
            {
                Row = row,
                Col = col,
                ImgRows = imgRows,
                ImgCols = imgCols,
                DefectCode = defectCodeOverride ?? v.Category,
                Confidence = v.Confidence,
                Region = v.Region
            };
    }
}
