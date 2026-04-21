using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HWKUltra.Core;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Extracts slider regions of interest (ROIs) from pocket images.
    /// Migrated verbatim from legacy WD.AVI.Vision.GetSliderROI.
    /// TODO: this file is very large (~2600 lines) and contains many product-specific
    /// magic constants; consider splitting / parameterizing in a follow-up task.
    /// </summary>
    static public class SliderRoiExtractor
    {
        /// <summary>
        /// Site code — controls color-channel handling path. Replaces the legacy
        /// ConfigurationManager.AppSettings["Site"] lookup. Default "THO".
        /// Valid values: "THO", "PHO", anything else falls through to the else branch.
        /// </summary>
        public static string Site { get; set; } = "THO";

        static public Bitmap GetROI(Bitmap bmp)
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
            margnRect.Size.Height = rotatedRect.Size.Height + 100;
            margnRect.Size.Width = rotatedRect.Size.Width + 100;

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
            return MatToBitmap(result);
        }

        public static Bitmap MatToBitmap(Mat mat)
        {
            mat.ConvertTo(mat, MatType.CV_8U);
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        }

        public static Bitmap ByteToBitmap(byte[] data, int height, int width)
        {
            Mat mat = new Mat(height, width, MatType.CV_8UC1, data);
            mat.ConvertTo(mat, MatType.CV_8U);
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        }

        static public Bitmap GetROI(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness)
        {
            sharpness = new double[5];
            sharpness[0] = 0;
            sharpness[1] = 0;
            sharpness[2] = 0;
            sharpness[3] = 0;
            sharpness[4] = 0;
            Mat source = new Mat(height, width, MatType.CV_8UC1, data);
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
            margnRect.Size.Height = rotatedRect.Size.Height + 100;
            margnRect.Size.Width = rotatedRect.Size.Width + 100;

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
            srcImg = MatToBitmap(result);//source

            Mat imgSobel = new Mat();
            Cv2.Sobel(result, imgSobel, MatType.CV_16U, 1, 1);

            /*
            Mat imgSobelx = new Mat();
            Mat imgSobely = new Mat();
            Cv2.Sobel(result, imgSobelx, MatType.CV_64F, 1, 0);
            Cv2.Sobel(result, imgSobely, MatType.CV_64F, 0, 1);
            Cv2.AddWeighted(imgSobelx, 0.5, imgSobely, 0.5, 0, imgSobel);
            Cv2.ConvertScaleAbs(imgSobel, imgSobel);
            */

            if (result.Width < 1500 || result.Height < 1500)
            {
                sharpness[0] = 0;
                sharpness[1] = 0;
                sharpness[2] = 0;
                sharpness[3] = 0;
                sharpness[4] = 0;
            }
            else
            {
                sharpness[0] = Cv2.Mean(imgSobel)[0];
                int wid = imgSobel.Width;
                int hei = imgSobel.Height;
                Rect roi = new Rect(0, 0, wid / 2, hei / 2);
                Mat corROI = new Mat(imgSobel, roi);
                sharpness[1] = Cv2.Mean(corROI)[0];

                roi = new Rect(wid / 2, 0, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[2] = Cv2.Mean(corROI)[0];

                roi = new Rect(0, hei / 2, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[3] = Cv2.Mean(corROI)[0];

                roi = new Rect(wid / 2, hei / 2, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[4] = Cv2.Mean(corROI)[0];
            }

            Mat result1 = new Mat();
            Cv2.Resize(result, result1, new OpenCvSharp.Size(640, 640));
            //Cv2.CvtColor(result, result1, ColorConversionCodes.GRAY2RGB);
            return MatToBitmap(result1);
            //var buffer = new VectorOfByte();
            //result1.ImEncode(".jpg",  buffer);  //Must use .jpg not jpg
            //byte[] jpgBytes = buffer.ToArray();

            //byte[] retData = new byte[1228800];
            //result1.Get<byte>( out retData);
        }



        static public Bitmap[] GetROIAndBatchImg(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness)
        {
            sharpness = new double[5];
            sharpness[0] = 0;
            sharpness[1] = 0;
            sharpness[2] = 0;
            sharpness[3] = 0;
            sharpness[4] = 0;
            Mat source = new Mat(height, width, MatType.CV_8UC1, data);
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
            margnRect.Size.Height = rotatedRect.Size.Height + 100;
            margnRect.Size.Width = rotatedRect.Size.Width + 100;

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
            srcImg = MatToBitmap(result);//source

            Mat imgSobel = new Mat();
            Cv2.Sobel(result, imgSobel, MatType.CV_16U, 1, 1);

            if (result.Width < 1500 || result.Height < 1500)
            {
                sharpness[0] = 0;
                sharpness[1] = 0;
                sharpness[2] = 0;
                sharpness[3] = 0;
                sharpness[4] = 0;
            }
            else
            {
                sharpness[0] = Cv2.Mean(imgSobel)[0];
                int wid = imgSobel.Width;
                int hei = imgSobel.Height;
                Rect roi = new Rect(0, 0, wid / 2, hei / 2);
                Mat corROI = new Mat(imgSobel, roi);
                sharpness[1] = Cv2.Mean(corROI)[0];

                roi = new Rect(wid / 2, 0, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[2] = Cv2.Mean(corROI)[0];

                roi = new Rect(0, hei / 2, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[3] = Cv2.Mean(corROI)[0];

                roi = new Rect(wid / 2, hei / 2, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[4] = Cv2.Mean(corROI)[0];
            }

            Bitmap[] ret = new Bitmap[4];
            Mat result1 = new Mat();
            Mat result2 = new Mat();
            Mat result3 = new Mat();
            Mat result4 = new Mat();
            Rect batchroi = new Rect(0, 0, 1280, 1280);
            result1 = new Mat(result, batchroi);
            ret[0] = MatToBitmap(result1);
            batchroi = new Rect(result.Width - 1280, 0, 1280, 1280);
            result2 = new Mat(result, batchroi);
            ret[1] = MatToBitmap(result2);
            batchroi = new Rect(0, result.Height - 1280, 1280, 1280);
            result3 = new Mat(result, batchroi);
            ret[2] = MatToBitmap(result3);
            batchroi = new Rect(result.Width - 1280, result.Height - 1280, 1280, 1280);
            result4 = new Mat(result, batchroi);
            ret[3] = MatToBitmap(result4);
            return ret;
        }

        static public void GetROIAndBatchImg(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness, out float[] src1, out float[] src2, out float[] src3, out float[] src4)
        {
            sharpness = new double[5];
            sharpness[0] = 0;
            sharpness[1] = 0;
            sharpness[2] = 0;
            sharpness[3] = 0;
            sharpness[4] = 0;
            Mat source = new Mat(height, width, MatType.CV_8UC1, data);
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
            margnRect.Size.Height = rotatedRect.Size.Height + 100;
            margnRect.Size.Width = rotatedRect.Size.Width + 100;

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
            srcImg = MatToBitmap(result);//source

            Mat imgSobel = new Mat();
            Cv2.Sobel(result, imgSobel, MatType.CV_16U, 1, 1);

            if (result.Width < 1500 || result.Height < 1500)
            {
                sharpness[0] = 0;
                sharpness[1] = 0;
                sharpness[2] = 0;
                sharpness[3] = 0;
                sharpness[4] = 0;
            }
            else
            {
                sharpness[0] = Cv2.Mean(imgSobel)[0];
                int wid = imgSobel.Width;
                int hei = imgSobel.Height;
                Rect roi = new Rect(0, 0, wid / 2, hei / 2);
                Mat corROI = new Mat(imgSobel, roi);
                sharpness[1] = Cv2.Mean(corROI)[0];

                roi = new Rect(wid / 2, 0, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[2] = Cv2.Mean(corROI)[0];

                roi = new Rect(0, hei / 2, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[3] = Cv2.Mean(corROI)[0];

                roi = new Rect(wid / 2, hei / 2, wid / 2, hei / 2);
                corROI = new Mat(imgSobel, roi);
                sharpness[4] = Cv2.Mean(corROI)[0];
            }

            Mat result1 = new Mat();
            Mat result2 = new Mat();
            Mat result3 = new Mat();
            Mat result4 = new Mat();
            Rect batchroi = new Rect(0, 0, 1280, 1280);
            result1 = new Mat(result, batchroi);
            src1 = BitmapToFloatArray(MatToBitmap(result1));
            batchroi = new Rect(result.Width - 1280, 0, 1280, 1280);
            result2 = new Mat(result, batchroi);
            src2 = BitmapToFloatArray(MatToBitmap(result2));
            batchroi = new Rect(0, result.Height - 1280, 1280, 1280);
            result3 = new Mat(result, batchroi);
            src3 = BitmapToFloatArray(MatToBitmap(result3));
            batchroi = new Rect(result.Width - 1280, result.Height - 1280, 1280, 1280);
            result4 = new Mat(result, batchroi);
            src4 = BitmapToFloatArray(MatToBitmap(result4));
        }


        public unsafe static byte[] GetROIEx(byte[] data, int height, int width)
        {
            Mat source = new Mat(height, width, MatType.CV_8UC1, data);
            Mat pro = new Mat();
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndices;

            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
            Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
            int size = 5;
            Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
            Cv2.Dilate(pro, pro, se);

            Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

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
            margnRect.Size.Height = rotatedRect.Size.Height + 100;
            margnRect.Size.Width = rotatedRect.Size.Width + 100;

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
            Cv2.Resize(result, result, new OpenCvSharp.Size(640, 640));
            //Cv2.CvtColor(result, result1, ColorConversionCodes.GRAY2RGB);
            //Bitmap aa = MatToBitmap(result);
            //return aa;
            byte[] pixels = new byte[640 * 640];
            index = 0;
            for (int i = 0; i < result.Rows; i++)
            {
                IntPtr a = result.Ptr(i);
                byte* b = (byte*)a.ToPointer();
                for (int j = 0; j < result.Cols; j++)
                {
                    pixels[index] = b[j];
                    index++;
                }
            }
            return pixels;
        }


        static private byte[] BitmapToBytes(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            byte[] bytes = new byte[ms.Length];
            ms.Read(bytes, 0, bytes.Length);
            ms.Dispose();
            return bytes;
        }

        private static byte[] GetBGRValues(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            var rowBytes = bmpData.Width * System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            var imgBytes = bmp.Height * rowBytes;
            byte[] rgbValues = new byte[imgBytes];
            IntPtr ptr = bmpData.Scan0;
            for (var i = 0; i < bmp.Height; i++)
            {
                Marshal.Copy(ptr, rgbValues, i * rowBytes, rowBytes);
                ptr += bmpData.Stride;
            }
            bmp.UnlockBits(bmpData);
            return rgbValues;
        }


        public static unsafe float[] MatToFloatArray(Mat mat)
        {
            float[] buffer = new float[mat.Width * mat.Height * mat.Channels()];
            IntPtr ptr = mat.Ptr();
            int pixelOffset = mat.Channels() * sizeof(float);
            float* data = (float*)ptr.ToPointer();
            fixed (float* bufferPtr = buffer)
            {
                Buffer.MemoryCopy(data, bufferPtr, buffer.Length * sizeof(float), mat.Width * mat.Height * mat.Channels() * sizeof(float));
            }
            return buffer;
        }

        public static float[] BitmapToFloatArray(Bitmap bmp)
        {
            int pixels = bmp.Width * bmp.Height;
            float[] buffer = new float[pixels * 3];
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* pixelPtr = (byte*)bmpData.Scan0;

                for (int i = 0; i < pixels; i++)
                {
                    buffer[i] = (float)pixelPtr[2] / 255.0f;
                    buffer[i + pixels] = (float)pixelPtr[1] / 255.0f;
                    buffer[i + pixels * 2] = (float)pixelPtr[0] / 255.0f;

                    pixelPtr += 3;
                }
            }

            bmp.UnlockBits(bmpData);

            return buffer;
        }
    }
}
