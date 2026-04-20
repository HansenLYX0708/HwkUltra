using System.Drawing;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Focus
{
    /// <summary>
    /// Focus metric based on mean of Laplacian response. Higher = sharper.
    /// Migrated from legacy WD.AVI.Vision.SharpnessLaplacian.
    /// </summary>
    public static class SharpnessLaplacian
    {
        public static double Get(Bitmap bmp)
        {
            Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat laplacian = new Mat();
            Cv2.Laplacian(source, laplacian, MatType.CV_16U);
            return Cv2.Mean(laplacian)[0];
        }
    }
}
