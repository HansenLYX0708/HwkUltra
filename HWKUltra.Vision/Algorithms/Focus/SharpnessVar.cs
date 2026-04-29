using System.Drawing;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Focus
{
    /// <summary>
    /// Focus metric based on per-pixel standard deviation. Higher = sharper.
    /// Migrated from legacy WD.AVI.Vision.SharpnessVar.
    /// </summary>
    public static class SharpnessVar
    {
        public static double Get(Bitmap bmp)
        {
            using Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            using Mat var = new Mat();
            using Mat mean = new Mat();
            Cv2.MeanStdDev(source, mean, var);
            return Cv2.Mean(var)[0];
        }
    }
}
