using OpenCvSharp;
using System;
using System.Drawing;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Locates sliders / pole tips inside a pocket image.
    /// Migrated from legacy WD.AVI.Vision.FindSliderInPocket.
    /// NOTE: <see cref="FindPoleTip"/> preserves a legacy behavior where the return
    /// flag <c>ret</c> is never set to true — this is the original semantic; the
    /// caller relies on the out-parameter center coordinates instead. TODO: verify intent.
    /// </summary>
    public static class SliderFinder
    {
        public static System.Drawing.Point Find(Bitmap bmp)
        {
            Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat pro = new Mat();
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndices;

            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
            Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
            int size = 5;
            Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            Cv2.Dilate(pro, pro, se);

            Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            if (contours.Length <= 0)
            {
                throw new Exception("Can't find slider.");
            }

            int index = 0;
            double contour_len = Cv2.ArcLength(contours[0], true);
            for (int i = 0; i < contours.Length; i++)
            {
                double len_tmp = Cv2.ArcLength(contours[i], true);
                if (len_tmp > contour_len)
                {
                    contour_len = len_tmp;
                    index = i;
                }
            }

            RotatedRect rotatedRect = Cv2.MinAreaRect(contours[index]);
            RotatedRect margnRect = new RotatedRect();
            margnRect.Angle = rotatedRect.Angle;
            margnRect.Center = rotatedRect.Center;

            System.Drawing.Point datumPoint = new System.Drawing.Point(1, 1);

            return datumPoint;
        }

        public static bool FindPoleTip(byte[] data, int height, int width, bool save, out float x, out float y)
        {
            bool ret = false;
            x = 0;
            y = 0;
            Mat source = new Mat(height, width, MatType.CV_8UC1, data);
            Mat pro = new Mat();
            Cv2.Threshold(source, pro, 0, 255, ThresholdTypes.Otsu);
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndices;
            Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

            if (contours.Length == 0) return false;

            int index = 0;
            double contour_len = Cv2.ArcLength(contours[0], true);
            for (int i = 0; i < contours.Length; i++)
            {
                double len_tmp = Cv2.ArcLength(contours[i], true);
                if (len_tmp > contour_len)
                {
                    contour_len = len_tmp;
                    index = i;
                }
            }

            RotatedRect minRect = Cv2.MinAreaRect(contours[index]);

            RotatedRect tmpRect = new RotatedRect();
            if (minRect.Size.Width > minRect.Size.Height)
            {
                if (save)
                {
                    tmpRect.Angle = minRect.Angle;
                    tmpRect.Center = new Point2f(
                        (float)(minRect.Center.X - minRect.Size.Width * (11.5 / 25.0) * Math.Cos(minRect.Angle * Math.PI / 180.0)),
                        (float)(minRect.Center.Y - minRect.Size.Width * (11.5 / 25.0) * Math.Sin(minRect.Angle * Math.PI / 180.0)));
                    tmpRect.Size = new Size2f(60, 60);
                    Point2f[] points = tmpRect.Points();
                    for (int j = 0; j < 4; j++)
                        Cv2.Line(source, points[j].ToPoint(), points[(j + 1) % 4].ToPoint(), new Scalar(0, 0, 255));
                    Cv2.ImWrite("findPoleTip.bmp", source);
                }
                x = tmpRect.Center.X;
                y = tmpRect.Center.Y;
            }
            return ret;
        }
    }
}
