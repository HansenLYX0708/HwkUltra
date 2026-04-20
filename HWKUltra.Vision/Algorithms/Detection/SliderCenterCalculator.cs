using System.Drawing;
using HWKUltra.Core;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Computes the geometric center of a slider within a pocket image by
    /// extracting the largest contour and its min-area rectangle.
    /// Migrated from legacy WD.AVI.Vision.GetSliderCenter.
    /// TODO: the hardcoded 1800..2300 / 1500..1900 bounds are slider-specific;
    /// consider parameterizing.
    /// </summary>
    public static class SliderCenterCalculator
    {
        public static Point3D CalcCenter(Bitmap bmp)
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

            if (contours.Length == 0) return new Point3D(0, 0, 0);

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
            margnRect.Size = new Size2f(rotatedRect.Size.Width + 100, rotatedRect.Size.Height + 100);

            Point2f[] dstPt = new Point2f[4];
            dstPt[0] = new Point2f(0, margnRect.Size.Height);
            dstPt[1] = new Point2f(0, 0);
            dstPt[2] = new Point2f(margnRect.Size.Width, 0);
            dstPt[3] = new Point2f(margnRect.Size.Width, margnRect.Size.Height);

            Mat result = new Mat();
            Mat m = Cv2.GetPerspectiveTransform(margnRect.Points(), dstPt);
            Cv2.WarpPerspective(source, result, m, new OpenCvSharp.Size(margnRect.Size.Width, margnRect.Size.Height));

            if (result.Height > result.Width)
            {
                Cv2.Transpose(result, result);
                Cv2.Flip(result, result, FlipMode.Y);
            }
            return new Point3D(margnRect.Center.X, margnRect.Center.Y, 0);
        }

        public static Point3D CalcCenter(byte[] data, int height, int width, string path, bool isColor = false)
        {
            Mat source;
            if (isColor)
            {
                source = new Mat(height, width, MatType.CV_8UC3, data);
                Cv2.CvtColor(source, source, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                source = new Mat(height, width, MatType.CV_8UC1, data);
            }
            Mat pro = new Mat();
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndices;

            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
            Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
            int size = 5;
            Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            Cv2.Dilate(pro, pro, se);

            Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

            if (contours.Length == 0) return new Point3D(width / 2.0, height / 2.0, 0.0);

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
            margnRect.Size = new Size2f(rotatedRect.Size.Width + 100, rotatedRect.Size.Height + 100);
            Point2f[] dstPt = new Point2f[4];
            dstPt[0] = new Point2f(0, margnRect.Size.Height);
            dstPt[1] = new Point2f(0, 0);
            dstPt[2] = new Point2f(margnRect.Size.Width, 0);
            dstPt[3] = new Point2f(margnRect.Size.Width, margnRect.Size.Height);

            Mat result = new Mat();
            Mat m = Cv2.GetPerspectiveTransform(margnRect.Points(), dstPt);
            Cv2.WarpPerspective(source, result, m, new OpenCvSharp.Size(margnRect.Size.Width, margnRect.Size.Height));

            if (result.Height > result.Width)
            {
                Cv2.Transpose(result, result);
                Cv2.Flip(result, result, FlipMode.Y);
            }

            if (result.Width < 1800 || result.Width > 2300)
                return new Point3D(width / 2.0, height / 2.0, 0.0);

            if (result.Height < 1500 || result.Height > 1900)
                return new Point3D(width / 2.0, height / 2.0, 0.0);

            return new Point3D(margnRect.Center.X, margnRect.Center.Y, 0);
        }
    }
}
