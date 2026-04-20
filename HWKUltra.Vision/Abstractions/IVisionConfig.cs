namespace HWKUltra.Vision.Abstractions
{
    /// <summary>
    /// Configuration lookup used by rendering algorithms.
    /// Replaces legacy WD.AVI.Common.UtilsCfg.Category2rgb.
    /// </summary>
    public interface IVisionConfig
    {
        /// <summary>Category string → color mapping for drawing defect boxes.</summary>
        IReadOnlyDictionary<string, VisionColor> CategoryColors { get; }
    }

    /// <summary>
    /// Simple default implementation backed by a dictionary.
    /// </summary>
    public class VisionConfig : IVisionConfig
    {
        public IReadOnlyDictionary<string, VisionColor> CategoryColors { get; }

        public VisionConfig(IReadOnlyDictionary<string, VisionColor> categoryColors)
        {
            CategoryColors = categoryColors;
        }
    }
}
