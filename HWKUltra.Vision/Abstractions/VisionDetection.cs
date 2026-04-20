using HWKUltra.Core;

namespace HWKUltra.Vision.Abstractions
{
    /// <summary>
    /// A single raw detection produced by a vision algorithm or DL inference.
    /// Pure and slot-agnostic: the Flow layer enriches this with tray Row/Col
    /// and wraps it into a <c>HWKUltra.TestRun.Reports.DefectDetail</c>.
    /// </summary>
    public class VisionDetection
    {
        /// <summary>Category label (e.g., "A2", "A5", "scratch", ...).</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Confidence in [0, 1].</summary>
        public float Confidence { get; set; }

        /// <summary>Axis-aligned bounding box in the inspected image.</summary>
        public BoundingBox Region { get; set; } = new BoundingBox(0, 0, 0, 0);
    }
}
