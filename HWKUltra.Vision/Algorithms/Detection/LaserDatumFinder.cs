using HWKUltra.Core;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Locates the center of a laser datum dot by min-enclosing-circle over the largest contour.
    /// Migrated from legacy WD.AVI.Vision.FindLaserDatum.
    /// Debug image writes are now gated behind <paramref name="saveImg"/>.
    /// </summary>
    public static class LaserDatumFinder
    {
        public static Point3D GetCenter(byte[] data, int height, int width, string path, bool saveImg = false)
        {
            var source = new Mat(height, width, MatType.CV_8UC1, data);
            if (saveImg) Cv2.ImWrite("src.bmp", source);
            Mat pro = new Mat();
            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(21, 21), 1.5);

            int size = 31;
            Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            Cv2.Dilate(pro, pro, se);
            if (saveImg) Cv2.ImWrite("pro.bmp", pro);

            Mat test = new Mat();
            Cv2.Threshold(pro, test, 0, 255, ThresholdTypes.Otsu);
            if (saveImg) Cv2.ImWrite("thr2.bmp", test);
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndices;
            Cv2.FindContours(test, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            if (contours.Length == 0)
                return new Point3D(0, 0, 0);

            int index = 0;
            double contour_len = Cv2.ArcLength(contours[0], true);
            Point2f center;
            float radius;
            Cv2.CvtColor(source, test, ColorConversionCodes.GRAY2RGB);

            for (int i = 0; i < contours.Length; i++)
            {
                double len_tmp = Cv2.ArcLength(contours[i], true);
                if (len_tmp > contour_len)
                {
                    contour_len = len_tmp;
                    index = i;
                }
            }
            Cv2.MinEnclosingCircle(contours[index], out center, out radius);
            Cv2.Circle(test, (int)center.X, (int)center.Y, (int)radius, new Scalar(0, 255, 0), 2, LineTypes.AntiAlias);
            if (saveImg) Cv2.ImWrite("enclosing.bmp", test);

            return new Point3D(center.X, center.Y, 0);
        }
    }
}
