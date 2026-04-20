using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Calibration
{
    /// <summary>
    /// Generates a crosshair-plus-circle template image used by <c>DatumFinder</c>.
    /// Migrated from legacy WD.AVI.Vision.CreateDatumTemplate.
    /// </summary>
    public static class DatumTemplateGenerator
    {
        /// <summary>
        /// Create and save a template image. Defaults to "DatumTemplate.bmp" in CWD
        /// to preserve legacy behavior; pass <paramref name="savePath"/> to override.
        /// </summary>
        public static void Create(int rows, int cols, int radius, int gap, int barWidth, string savePath = "DatumTemplate.bmp")
        {
            Mat blackBackground = Mat.Zeros(rows, cols, MatType.CV_8UC1);

            Cv2.Circle(blackBackground, new OpenCvSharp.Point(cols / 2, rows / 2), radius, Scalar.White, -1);

            Cv2.Rectangle(blackBackground, new OpenCvSharp.Point(0, rows / 2 - barWidth), new OpenCvSharp.Point(cols / 2 - gap, rows / 2 + barWidth), Scalar.White, -1);
            Cv2.Rectangle(blackBackground, new OpenCvSharp.Point(cols / 2 + gap, rows / 2 - barWidth), new OpenCvSharp.Point(cols, rows / 2 + barWidth), Scalar.White, -1);
            Cv2.Rectangle(blackBackground, new OpenCvSharp.Point(cols / 2 - barWidth, 0), new OpenCvSharp.Point(cols / 2 + barWidth, rows / 2 - gap), Scalar.White, -1);
            Cv2.Rectangle(blackBackground, new OpenCvSharp.Point(cols / 2 - barWidth, rows / 2 + gap), new OpenCvSharp.Point(cols / 2 + barWidth, rows), Scalar.White, -1);
            blackBackground = 255 - blackBackground;
            Cv2.ImWrite(savePath, blackBackground);
        }
    }
}
