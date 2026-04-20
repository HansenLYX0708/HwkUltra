using System;
using System.Drawing;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Focus
{
    /// <summary>
    /// Tenengrad-style focus metric using Sobel gradients.
    /// Migrated from legacy WD.AVI.Vision.{CalculateTG, TenengradGradient}.
    /// </summary>
    public class CalculateTG
    {
        public double Getvalue(Bitmap bmp)
        {
            try
            {
                Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                Mat imgSobel = new Mat();
                Cv2.Sobel(source, imgSobel, MatType.CV_16U, 1, 1);
                double ret = Cv2.Mean(imgSobel)[0];
                return ret;
            }
            catch
            {
                return 0;
            }
        }
    }

    public static class TenengradGradient
    {
        public static double Get(Bitmap bmp)
        {
            try
            {
                Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                Mat imgSobel = new Mat();
                Cv2.Sobel(source, imgSobel, MatType.CV_16U, 1, 1);
                double ret = Cv2.Mean(imgSobel)[0];
                return ret;
            }
            catch
            {
                return 0;
            }
        }

        public static double Get(Bitmap bmp, int width, int height)
        {
            Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat imgSobel = new Mat();
            Cv2.Resize(source, source, new OpenCvSharp.Size(width, height));
            Cv2.Sobel(source, imgSobel, MatType.CV_16U, 1, 1);
            return Cv2.Mean(imgSobel)[0];
        }

        public static double Get(Bitmap bmp, System.Drawing.Point datumPos)
        {
            try
            {
                Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                Rect roi = new Rect(datumPos.X / 2, datumPos.Y / 2, datumPos.X, datumPos.Y);
                Mat sourceROI = new Mat(source, roi);
                Mat imgSobel = new Mat();
                Cv2.Sobel(sourceROI, imgSobel, MatType.CV_16U, 1, 1);
                return Cv2.Mean(imgSobel)[0];
            }
            catch
            {
                return 0;
            }
        }

        public static double DatumGet(Bitmap bmp, System.Drawing.Point datumPos)
        {
            try
            {
                Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                Rect roi = new Rect(datumPos.X / 2, datumPos.Y / 2, datumPos.X, datumPos.Y);
                Mat sourceROI = new Mat(source, roi);
                Mat imgSobel = new Mat();
                Cv2.Sobel(sourceROI, imgSobel, MatType.CV_16U, 1, 1, 11);
                return Cv2.Mean(imgSobel)[0];
            }
            catch
            {
                return 0;
            }
        }

        public static double Get(byte[] data, int height, int width, string path, bool isColor = false)
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

            Rect roi = new Rect(width / 4, height / 4, width / 2, height / 2);
            Mat sourceROI = new Mat(source, roi);
            Mat imgSobelx = new Mat();
            Mat imgSobely = new Mat();
            Cv2.Sobel(sourceROI, imgSobelx, MatType.CV_64F, 1, 0);
            Cv2.Sobel(sourceROI, imgSobely, MatType.CV_64F, 0, 1);
            Mat absGx = new Mat();
            Mat absGy = new Mat();
            Cv2.Absdiff(imgSobelx, Scalar.All(0), absGx);
            Cv2.Absdiff(imgSobely, Scalar.All(0), absGy);
            Mat mag = absGx + absGy;
            double sum = Cv2.Sum(mag).Val0;
            return sum / (source.Rows * source.Cols);
        }

        public static double[] Get5(Bitmap bmp)
        {
            double[] ret = { 0, 0, 0, 0, 0 };
            try
            {
                Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                Mat imgSobel = new Mat();
                Cv2.Sobel(source, imgSobel, MatType.CV_16U, 1, 1);

                ret[4] = Cv2.Mean(imgSobel)[0];

                int wid = imgSobel.Width;
                int hei = imgSobel.Height;
                Rect roi = new Rect(0, 0, wid / 2, hei / 2);
                Mat sourceROI = new Mat(imgSobel, roi);
                ret[0] = Cv2.Mean(sourceROI)[0];

                roi = new Rect(wid / 2, 0, wid / 2, hei / 2);
                sourceROI = new Mat(imgSobel, roi);
                ret[1] = Cv2.Mean(sourceROI)[0];

                roi = new Rect(0, hei / 2, wid / 2, hei / 2);
                sourceROI = new Mat(imgSobel, roi);
                ret[2] = Cv2.Mean(sourceROI)[0];

                roi = new Rect(wid / 2, hei / 2, wid / 2, hei / 2);
                sourceROI = new Mat(imgSobel, roi);
                ret[3] = Cv2.Mean(sourceROI)[0];

                return ret;
            }
            catch
            {
                return ret;
            }
        }

        public static double GetColor(Mat src)
        {
            Mat dst = new Mat();
            Cv2.CvtColor(src, dst, ColorConversionCodes.BGR2GRAY);
            Cv2.Sobel(dst, dst, MatType.CV_16U, 1, 1);
            return Cv2.Mean(dst)[0];
        }
    }
}
