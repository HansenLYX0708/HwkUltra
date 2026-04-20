using HWKUltra.Core;

namespace HWKUltra.TestRun.Reports
{
    /// <summary>
    /// A single defect found during inspection of a pocket/slot.
    /// Relocated from HWKUltra.Tray as part of v3 refactor:
    /// pure inspection/report data — not a device concept.
    /// DefectCode is a string (loaded from JSON config, not a hardcoded enum).
    /// </summary>
    public class DefectDetail
    {
        /// <summary>1-based row in the tray grid.</summary>
        public int Row { get; set; }

        /// <summary>1-based column in the tray grid.</summary>
        public int Col { get; set; }

        /// <summary>Category code from vision/AI (e.g., "A2", "A5", ...).</summary>
        public string DefectCode { get; set; } = "";

        /// <summary>Axis-aligned bounding box in the inspected image (optional).</summary>
        public BoundingBox? Region { get; set; }

        /// <summary>Detection confidence in [0,1].</summary>
        public float Confidence { get; set; }

        /// <summary>Inspected image height in pixels (used for coordinate rotation on save).</summary>
        public int ImgRows { get; set; }

        /// <summary>Inspected image width in pixels.</summary>
        public int ImgCols { get; set; }
    }
}
