using System.Collections.Generic;
using System.Drawing;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Calibration
{
    /// <summary>
    /// Computes millimeters-per-pixel (MPP) from a circle-grid calibration target.
    /// Migrated from legacy WD.AVI.Vision.CalibrationCameraMPP.
    /// TODO: verify whether the 17.0 (Bitmap path) and 0.17 (byte[] path) factors
    /// are intentional; preserved verbatim for bit-exact parity.
    /// </summary>
    public static class CameraMppCalibrator
    {
        public static double GetMPP(Bitmap bmp)
        {
            Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat pro = new Mat();

            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
            int size = 5;
            Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            Cv2.Dilate(pro, pro, se);
            CircleSegment[] cs = Cv2.HoughCircles(pro, HoughModes.Gradient, 1, 30, 70, 70, 85, 100);

            List<double> radiusFilter = new List<double>();
            for (int i = 0; i < cs.Length; i++)
            {
                for (int j = i + 1; j < cs.Length; j++)
                {
                    double distance = System.Math.Sqrt(System.Math.Pow(cs[i].Center.X - cs[j].Center.X, 2) + System.Math.Pow(cs[i].Center.Y - cs[j].Center.Y, 2));
                    if (distance > 390 && distance < 420)
                    {
                        radiusFilter.Add(distance);
                    }
                }
            }
            double sum = 0;
            for (int k = 0; k < radiusFilter.Count; k++)
            {
                sum += radiusFilter[k];
            }
            double ave = sum / radiusFilter.Count;
            double mpp = 17.0 / ave;
            return mpp;
        }

        public static double GetMPP(byte[] data, int height, int width, string path, bool isColor)
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

            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
            int size = 5;
            _ = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            CircleSegment[] cs = Cv2.HoughCircles(pro, HoughModes.Gradient, 1, 30, 70, 70, 85, 100);

            List<double> radiusFilter = new List<double>();
            for (int i = 0; i < cs.Length; i++)
            {
                for (int j = i + 1; j < cs.Length; j++)
                {
                    double distance = System.Math.Sqrt(System.Math.Pow(cs[i].Center.X - cs[j].Center.X, 2) + System.Math.Pow(cs[i].Center.Y - cs[j].Center.Y, 2));
                    if (distance > 390 && distance < 420)
                    {
                        radiusFilter.Add(distance);
                    }
                }
            }
            double sum = 0;
            if (radiusFilter.Count == 0)
            {
                return 0;
            }
            else
            {
                for (int k = 0; k < radiusFilter.Count; k++)
                {
                    sum += radiusFilter[k];
                }
                double ave = sum / radiusFilter.Count;
                double mpp = 0.17 / ave;
                return mpp;
            }
        }
    }
}
