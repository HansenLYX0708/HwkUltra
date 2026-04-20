using System.Drawing;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Rendering
{
    /// <summary>
    /// Convenience helpers for saving raw byte buffers / bitmaps to disk.
    /// Migrated from legacy WD.AVI.Vision.ImageFormatHelper.
    /// </summary>
    public static class ImageFormatHelper
    {
        public static void ToGrayBmp(Bitmap bmp, string path)
        {
            Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Cv2.ImWrite(path, source);
        }

        public static void ToGrayBmp(byte[] data, int height, int width, string path)
        {
            var source = new Mat(height, width, MatType.CV_8UC1, data);
            Cv2.ImWrite(path, source);
        }
    }
}
