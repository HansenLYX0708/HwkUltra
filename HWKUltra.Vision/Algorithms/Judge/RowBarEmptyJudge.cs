using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Judge
{
    /// <summary>
    /// Decides whether a row-bar image is empty (no slider).
    /// Returns true if the largest contour's min-area-rect height is &lt;= 50 pixels, else false.
    /// Migrated from legacy WD.AVI.Vision.JudgeRowBarEmpty.
    /// </summary>
    public class RowBarEmptyJudge
    {
        public bool Judge(byte[] data, int height, int width, bool isColor = false, bool saveImg = false)
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

            if (saveImg) Cv2.ImWrite("Triangle.bmp", pro);

            int size = 5;
            Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            Cv2.Dilate(pro, pro, se);

            if (saveImg) Cv2.ImWrite("Dilate.bmp", pro);

            Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

            if (contours.Length > 0)
            {
                int index = -1;
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

                if (saveImg)
                {
                    Mat blackImage = new Mat(new OpenCvSharp.Size(source.Width, source.Height), MatType.CV_8UC3, Scalar.Black);
                    double fontScale = 0.5;
                    Scalar textcolor = new Scalar(255, 255, 255);
                    Scalar contourcolor = new Scalar(100, 100, 100);
                    Scalar keycontourcolor = new Scalar(0, 0, 255);

                    for (int i = 0; i < contours.Length; i++)
                    {
                        double len = Cv2.ArcLength(contours[i], true);
                        double area = Cv2.ContourArea(contours[i]);
                        if (area > 20000)
                        {
                            if (len > 2000 && len < 3800 && area > 10000 && area < 330000)
                                Cv2.DrawContours(blackImage, contours, i, keycontourcolor, 2);
                            else
                                Cv2.DrawContours(blackImage, contours, i, contourcolor, 2);
                            Moments moments = Cv2.Moments(contours[i]);
                            var center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
                            Cv2.PutText(blackImage, "Area: " + area.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y), HersheyFonts.HersheySimplex, fontScale, textcolor);
                            Cv2.PutText(blackImage, "Len: " + len.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y + 30), HersheyFonts.HersheySimplex, fontScale, textcolor);
                        }
                    }
                    Cv2.ImWrite("ErodeWithContours.bmp", blackImage);
                }

                if (index < 0) return true;

                RotatedRect rotatedRect = Cv2.MinAreaRect(contours[index]);
                return rotatedRect.Size.Height <= 50;
            }
            return true;
        }
    }
}
