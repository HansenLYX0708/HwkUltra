using System.Drawing;

namespace HWKUltra.Vision.Abstractions
{
    /// <summary>
    /// Abstraction over a deep-learning inference engine.
    /// Allows production code to run against a native backend
    /// and tests to run against a simulated backend.
    /// </summary>
    public interface IInferenceEngine
    {
        /// <summary>
        /// True if the engine is ready to serve predictions.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Load / warm up the model. Safe to call multiple times.
        /// </summary>
        void LoadModel();

        /// <summary>
        /// Run inference on a single image.
        /// Returns the raw native result array (typically 256 int slots, legacy format).
        /// </summary>
        int[] Predict(Bitmap bmp);
    }
}
