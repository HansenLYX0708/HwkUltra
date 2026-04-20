using OpenCvSharp;
using System.Drawing;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Finds the crosshair-plus-circle datum marker in an image.
    /// Migrated from legacy WD.AVI.Vision.FindDatum with two safety improvements:
    /// 1. Template is now loaded lazily (once, on first use) rather than in a static
    ///    type initializer that crashed the app at load time if the file was missing.
    /// 2. Template path is overridable.
    /// </summary>
    public static class DatumFinder
    {
        private static Mat? _template;
        private static string _templatePath = "DatumTemplate.bmp";
        private static readonly object _templateLock = new();

        /// <summary>Override the template path (must be set before first Find call).</summary>
        public static void SetTemplatePath(string path)
        {
            lock (_templateLock)
            {
                _templatePath = path;
                _template = null; // force reload
            }
        }

        private static Mat GetTemplate()
        {
            lock (_templateLock)
            {
                if (_template is null || _template.Empty())
                {
                    _template = Cv2.ImRead(_templatePath, ImreadModes.Grayscale);
                    if (_template.Empty())
                        throw new System.IO.FileNotFoundException(
                            $"Datum template not found at '{_templatePath}'. Call DatumTemplateGenerator.Create first.");
                }
                return _template;
            }
        }

        public static System.Drawing.Point Find(Bitmap bmp, bool saveImg = false)
        {
            var templateImg = GetTemplate();
            Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat pro = new Mat();

            Mat matchResult = new Mat();

            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
            Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
            int size = 9;
            Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            Cv2.Dilate(pro, pro, se);

            Cv2.MatchTemplate(pro, templateImg, matchResult, TemplateMatchModes.SqDiff);

            Cv2.Normalize(matchResult, matchResult, 1, 0, NormTypes.MinMax, -1);
            OpenCvSharp.Point minLocation;
            Cv2.MinMaxLoc(matchResult, out minLocation, out _);
            Cv2.Rectangle(source, minLocation, new OpenCvSharp.Point(minLocation.X + templateImg.Cols, minLocation.Y + templateImg.Rows), Scalar.Black, 3);
            Cv2.Circle(source, new OpenCvSharp.Point(minLocation.X + templateImg.Cols / 2, minLocation.Y + templateImg.Rows / 2), 5, Scalar.White, -1);
            System.Drawing.Point datumPoint = new System.Drawing.Point(minLocation.X + templateImg.Cols / 2, minLocation.Y + templateImg.Rows / 2);

            if (saveImg)
            {
                Cv2.ImWrite("DatumWithResult.bmp", source);
            }
            return datumPoint;
        }

        public static bool FindDatumOn2X(byte[] data, int height, int width, bool save, out float x, out float y)
        {
            bool ret = false;
            x = 0;
            y = 0;
            Mat source = new Mat(height, width, MatType.CV_8UC1, data);
            Mat pro = new Mat();
            Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndices;

            RotatedRect[] rects = new RotatedRect[2];
            int rects_index = 0;
            Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area > 10000 && area < 12000)
                {
                    RotatedRect roR = Cv2.MinAreaRect(contours[i]);
                    if (roR.Size.Width > roR.Size.Height)
                    {
                        if (rects_index >= 2)
                        {
                            rects_index++;
                            break;
                        }
                        rects[rects_index++] = roR;
                        if (save)
                        {
                            Cv2.DrawContours(source, contours, i, new Scalar(255, 0, 0));
                            Point2f[] points = roR.Points();
                            for (int j = 0; j < 4; j++)
                                Cv2.Line(source, points[j].ToPoint(), points[(j + 1) % 4].ToPoint(), new Scalar(0, 0, 255));
                        }
                    }
                }
            }
            if (rects_index == 2)
            {
                Point2f center = (rects[0].Center + rects[1].Center);
                x = center.X / 2;
                y = center.Y / 2;
                ret = true;
            }

            if (save)
                Cv2.ImWrite("FindDatumEllipseResult.bmp", pro);

            return ret;
        }

        public static bool FindEllipse(byte[] data, int height, int width, bool save, out float x, out float y)
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
            if (contours[index].Length < 5) return false;
            RotatedRect box = Cv2.FitEllipse(contours[index]);
            if (box.Center.X != 0 && box.Center.Y != 0)
            {
                x = box.Center.X;
                y = box.Center.Y;
                ret = true;
            }

            if (save)
            {
                Cv2.Ellipse(pro, box, new Scalar(0, 0, 255));
                Cv2.ImWrite("FindEllipseResult.bmp", pro);
            }

            return ret;
        }
    }
}
