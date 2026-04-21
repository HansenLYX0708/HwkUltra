using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HWKUltra.Core;
using HWKUltra.Vision.Algorithms.Focus;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Version-2 slider-ROI extractor with head-type detection and per-orientation logic.
    /// Split out of the legacy SliderRoiExtractor.cs (GetSliderROI.cs) for maintainability.
    /// </summary>
    public class GetSliderROIV2
    {
        /// <summary>
        /// orientation = 1  left,  orientation = 2 right
        /// </summary>
        public int orientation = 0;
        public int backside = 0;
        public int IsTypeWrong = 0;
        public int IsBlurry = 0;

        public string HeadType = "";

        public bool IsOpenDetect = true;

        public bool IsDetectHeadType = false;

        // public HRotatedRect historyPoletipResult;
        public GetSliderROIV2()
        {

        }

        public void GetROIAndBatchImg(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness, out float[] src1, out float[] src2, out float[] src3, out float[] src4)
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

        public void GetROI(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness, out float[] src1, bool saveImg = false)
        {
            try
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
                if (saveImg)
                {
                    Cv2.ImWrite("GaussianBlur.bmp", pro);
                }
                Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
                if (saveImg)
                {
                    Cv2.ImWrite("Otsu.bmp", pro);
                }

                //int sizeOpen = 3;
                //Mat seOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(sizeOpen, sizeOpen), new OpenCvSharp.Point(-1, -1));
                //Cv2.MorphologyEx(pro, pro, MorphTypes.Close, seOpen, new OpenCvSharp.Point(-1, -1), 1, BorderTypes.Constant, Scalar.Gold);
                //if (saveImg)
                //{
                //    Cv2.ImWrite("close.bmp", pro);
                //}
                int erodeSize = 41;
                Mat erodeSe = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(erodeSize, erodeSize), new OpenCvSharp.Point(-1, -1));
                Mat Erode = new Mat();
                Cv2.Erode(pro, Erode, erodeSe);
                if (saveImg)
                {
                    Cv2.ImWrite("Erode.bmp", Erode);
                }
                OpenCvSharp.Point[][] contoursKeep;
                Cv2.FindContours(Erode, out contoursKeep, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                double maxConcourArea = 0;
                for (int i = 0; i < contoursKeep.Length; i++)
                {
                    double area = Cv2.ContourArea(contoursKeep[i]);
                    if (area > maxConcourArea)
                    {
                        maxConcourArea = area;
                    }
                }

                if (saveImg)
                {
                    int k = 0;
                    Mat blackImage = new Mat(new OpenCvSharp.Size(3200, 2800), MatType.CV_8UC3, Scalar.Black);

                    OpenCvSharp.Point center;
                    double fontScale = 0.5;
                    Scalar textcolor = new Scalar(255, 255, 255);
                    Scalar contourcolor = new Scalar(100, 100, 100);
                    Scalar keycontourcolor = new Scalar(0, 0, 255);

                    for (int i = 0; i < contoursKeep.Length; i++)
                    {
                        double len = Cv2.ArcLength(contoursKeep[i], true);
                        double area = Cv2.ContourArea(contoursKeep[i]);
                        if (area > 20000)
                        {
                            k++;
                            if (len > 2000 && len < 3800 && area > 230000 && area < 330000)
                            {
                                Cv2.DrawContours(blackImage, contoursKeep, i, keycontourcolor, 2);
                            }
                            else
                            {
                                Cv2.DrawContours(blackImage, contoursKeep, i, contourcolor, 2);
                            }
                            Moments moments = Cv2.Moments(contoursKeep[i]);
                            center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));

                            // Draw text
                            Cv2.PutText(blackImage, "Area: " + area.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y), HersheyFonts.HersheySimplex, fontScale, textcolor);
                            Cv2.PutText(blackImage, "Len: " + len.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y + 30), HersheyFonts.HersheySimplex, fontScale, textcolor);
                        }
                    }
                    Cv2.ImWrite("ErodeWithContours.bmp", blackImage);
                }


                if (maxConcourArea > 2400000 && IsOpenDetect)
                {
                    backside = 1;
                    srcImg = MatToBitmap(source);
                    src1 = null;
                    return;

                }


                int size = 5;
                Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
                Cv2.Dilate(pro, pro, se);
                if (saveImg)
                {
                    Cv2.ImWrite("Dilate.bmp", pro);
                }
                Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                if (saveImg)
                {
                    Mat drawCon = Mat.Zeros(pro.Size(), source.Type());
                    for (int i = 0; i < contours.Length; i++)
                    {
                        Cv2.DrawContours(drawCon, contours, i, new Scalar(255, 255, 255), 2, LineTypes.Link8, hierarchyIndices);
                    }
                    Cv2.ImWrite("FindContours.bmp", drawCon);
                }


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

                // check slider ori
                Rect leftArea = new Rect(0, 0, 200, result.Height);
                Mat LeftAreaImg = new Mat(result, leftArea);
                if (saveImg)
                {
                    Cv2.ImWrite("left.bmp", LeftAreaImg);
                }
                Mat leftOtsu = new Mat();
                Cv2.GaussianBlur(LeftAreaImg, LeftAreaImg, new OpenCvSharp.Size(3, 3), 1.5);
                Cv2.Threshold(LeftAreaImg, leftOtsu, 0, 255, ThresholdTypes.Otsu);
                Cv2.FindContours(leftOtsu, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                int leftLen = contours.Length;
                if (saveImg)
                {
                    Cv2.ImWrite("leftOtsu.bmp", leftOtsu);
                }
                // double leftMean= Cv2.Mean(LeftAreaImg)[0];

                Rect rightArea = new Rect(result.Width - 200, 0, 200, result.Height);
                Mat RightAreaImg = new Mat(result, rightArea);
                if (saveImg)
                {
                    Cv2.ImWrite("right.bmp", RightAreaImg);
                }
                Mat rightOtsu = new Mat();
                Cv2.GaussianBlur(RightAreaImg, RightAreaImg, new OpenCvSharp.Size(3, 3), 1.5);
                Cv2.Threshold(RightAreaImg, rightOtsu, 0, 255, ThresholdTypes.Otsu);
                Cv2.FindContours(rightOtsu, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                int rightLen = contours.Length;
                if (saveImg)
                {
                    Cv2.ImWrite("rightOtsu.bmp", rightOtsu);
                }
                // double rightMean = Cv2.Mean(RightAreaImg)[0];

                if (rightLen > leftLen && false) // IsOpenDetect
                {
                    orientation = 1;
                    srcImg = MatToBitmap(source);
                    src1 = null;
                    return;
                }

                // check type AB
                /*
                var compare_A = Compare_SSIM("C:\\Users\\1000250081\\_work\\data\\ABType\\A.bmp", result);
                var compare_B = Compare_SSIM("C:\\Users\\1000250081\\_work\\data\\ABType\\B.bmp", result);

                int filterCount = 0;
                double topLen = 0;
                double topArea = 0;
                OpenCvSharp.Point topCenter = new OpenCvSharp.Point();
                double bottomLen = 0;
                double bottomArea = 0;
                OpenCvSharp.Point bottomCenter = new OpenCvSharp.Point();

                for (int i = 0; i < contoursKeep.Length; i++)
                {
                    double len = Cv2.ArcLength(contoursKeep[i], true);
                    double area = Cv2.ContourArea(contoursKeep[i]);
                    if (area > 20000)
                    {
                        if (len > 2000 && len < 3800 && area > 230000 && area < 330000)
                        {
                            filterCount++;
                            if (filterCount == 1)
                            {
                                topLen = len;
                                topArea = area;
                                Moments moments = Cv2.Moments(contoursKeep[i]);
                                topCenter = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
                            }
                            else if (filterCount == 2)
                            {
                                bottomLen = len;
                                bottomArea = area;
                                Moments moments = Cv2.Moments(contoursKeep[i]);
                                bottomCenter = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                        }
                    }
                }
                if (filterCount == 2)
                {
                    // verify top or bottom
                    if (topCenter.Y  > bottomCenter.Y + 311)
                    {
                        double tmpLen = topLen;
                        double tmpArea = topArea;
                        OpenCvSharp.Point tmpCenter = new OpenCvSharp.Point(topCenter.X, topCenter.Y);
                        topLen = bottomLen;
                        topArea = bottomArea;
                        topCenter = new OpenCvSharp.Point(bottomCenter.X, bottomCenter.Y);
                        bottomLen = tmpLen;
                        bottomArea = tmpArea;
                        bottomCenter = new OpenCvSharp.Point(tmpCenter.X, tmpCenter.Y);
                    }

                    if (topCenter.Y + 311 < bottomCenter.Y)
                    {
                        if (HeadType == "B")
                        {
                            if (topArea > bottomArea && topLen > bottomLen)
                            {
                                IsTypeWrong = 1;
                                srcImg = MatToBitmap(source);
                                src1 = null;
                                return;
                            }
                        }
                        else if (HeadType == "A")
                        {
                            if (topArea < bottomArea && topLen < bottomLen)
                            {
                                IsTypeWrong = 1;
                                srcImg = MatToBitmap(source);
                                src1 = null;
                                return;
                            }
                        }
                    }
                }
                */
                Rect middleArea = new Rect((int)(result.Width * 0.234) - 60, (int)(result.Height / 2) - 60, 120, 120);
                Mat middleAreaImg = new Mat(result, middleArea);
                if (saveImg)
                {
                    //Cv2.ImShow("middleArea", middleAreaImg);
                    Cv2.ImWrite("middleAreaImg.bmp", middleAreaImg);
                }
                Mat middleOtsu = new Mat();
                // Cv2.GaussianBlur(middleAreaImg, middleAreaImg, new OpenCvSharp.Size(7, 7), 1.5);
                Cv2.Threshold(middleAreaImg, middleOtsu, 83, 255, ThresholdTypes.Binary);
                Cv2.FindContours(middleOtsu, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                double midmaxConcourArea = 0;
                int maxConcourIndex = -1;
                if (contours.Length > 0)
                {
                    for (int i = 0; i < contours.Length; i++)
                    {
                        double area = Cv2.ContourArea(contours[i]);
                        if (area < 3600 && area > 900)
                        {
                            if (area > midmaxConcourArea)
                            {
                                midmaxConcourArea = area;
                                maxConcourIndex = i;
                            }
                        }
                    }
                    if (saveImg)
                    {
                        int k = 0;
                        Mat blackImage = new Mat(new OpenCvSharp.Size(middleAreaImg.Width, middleAreaImg.Height), MatType.CV_8UC3, Scalar.Black);

                        OpenCvSharp.Point center;
                        OpenCvSharp.Point2f center1;
                        double fontScale = 0.5;
                        Scalar textcolor = new Scalar(255, 255, 255);
                        Scalar contourcolor = new Scalar(0, 0, 255);
                        Scalar contourcolor1 = new Scalar(0, 255, 0);

                        for (int i = 0; i < contours.Length; i++)
                        {
                            double len = Cv2.ArcLength(contours[i], true);
                            double area = Cv2.ContourArea(contours[i]);
                            if (area < 3600 && area > 900)
                            {
                                k++;
                                Cv2.DrawContours(blackImage, contours, i, contourcolor, 1);
                                Moments moments = Cv2.Moments(contours[i]);
                                center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
                                center1 = Cv2.MinAreaRect(contours[i]).Center;
                                // Draw text
                                Cv2.Circle(blackImage, center, 2, contourcolor);
                                Cv2.Circle(blackImage, new OpenCvSharp.Point(center1.X, center1.Y), 2, contourcolor1);
                            }
                        }
                        Cv2.ImShow("ErodeWithContoursMid", blackImage);
                        Cv2.ImWrite("ErodeWithContoursMid.bmp", blackImage);
                    }

                    if (maxConcourIndex >= 0)
                    {
                        Moments minmoments = Cv2.Moments(contours[maxConcourIndex]);
                        RotatedRect midrotatedRect = Cv2.MinAreaRect(contours[maxConcourIndex]);
                        OpenCvSharp.Point2f momentcenter = new OpenCvSharp.Point((minmoments.M10 / minmoments.M00), (minmoments.M01 / minmoments.M00));
                        OpenCvSharp.Point2f rectcenter = midrotatedRect.Center;
                        if (HeadType == "B")
                        {
                            if (rectcenter.Y < momentcenter.Y && IsOpenDetect)
                            {
                                IsTypeWrong = 1;
                                srcImg = MatToBitmap(source);
                                src1 = null;
                                return;
                            }
                        }
                        else if (HeadType == "A")
                        {
                            if (rectcenter.Y > momentcenter.Y && IsOpenDetect)
                            {
                                IsTypeWrong = 1;
                                srcImg = MatToBitmap(source);
                                src1 = null;
                                return;
                            }
                        }
                    }
                }


                if (saveImg)
                {
                    //Cv2.ImShow("middleAreaOtsu", middleOtsu);
                    Cv2.ImWrite("middleAreaOtsu.bmp", middleOtsu);
                }



                if (saveImg)
                {
                    Cv2.ImWrite("sliderImage.bmp", result);
                    //Rect roitemp = new Rect(1653, 702, 300, 300);
                    //Mat corROItmp = new Mat(result, roitemp);
                    //Cv2.ImWrite("temp.bmp", corROItmp);
                }
                /*
                srcImg = MatToBitmap(result);//source
                */
                // Rotate image
                Mat rotateRet = new Mat();
                Cv2.Rotate(result, rotateRet, RotateFlags.Rotate90Clockwise);
                srcImg = MatToBitmap(rotateRet);//source

                Mat imgSobel = new Mat();
                Cv2.Sobel(result, imgSobel, MatType.CV_16U, 1, 1);
                if (saveImg)
                {
                    Cv2.ImWrite("sobel.bmp", imgSobel);
                }
                Cv2.Threshold(imgSobel, imgSobel, 0, 255, ThresholdTypes.Otsu);
                if (saveImg)
                {
                    Cv2.ImWrite("sobelOtsu.bmp", imgSobel);
                }

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
                Cv2.Resize(result, result1, new OpenCvSharp.Size(1280, 1280));
                //Mat[] channels = { result1, result1, result1 };
                //Mat result2 = new Mat();
                //Cv2.Merge(channels, result2);
                // src1 = MatToFloatArray(result2);
                //src1 = result2;
                src1 = BitmapToFloatArray(MatToBitmap(result1));
            }
            catch (Exception e)
            {
                throw new Exception("Find Slider error:" + e.Message);
            }
        }

        public Scalar Compare_SSIM(string imgFile1, Mat image2Tmp)
        {
            var image1 = Cv2.ImRead(imgFile1, ImreadModes.Grayscale);
            // 将两个图片处理成同样大小，否则会有错误： The operation is neither 'array op array' (where arrays have the same size and the same number of channels), nor 'array op scalar', nor 'scalar op array'
            var image2 = new Mat();
            Cv2.Resize(image2Tmp, image2, new OpenCvSharp.Size(image1.Size().Width, image1.Size().Height));
            double C1 = 6.5025, C2 = 58.5225;
            var validImage1 = new Mat();
            var validImage2 = new Mat();
            image1.ConvertTo(validImage1, MatType.CV_32F); //数据类型转换为 float,防止后续计算出现错误
            image2.ConvertTo(validImage2, MatType.CV_32F);


            Mat image1_1 = validImage1.Mul(validImage1); //图像乘积
            Mat image2_2 = validImage2.Mul(validImage2);
            Mat image1_2 = validImage1.Mul(validImage2);

            Mat gausBlur1 = new Mat(), gausBlur2 = new Mat(), gausBlur12 = new Mat();
            Cv2.GaussianBlur(validImage1, gausBlur1, new OpenCvSharp.Size(11, 11), 1.5); //高斯卷积核计算图像均值
            Cv2.GaussianBlur(validImage2, gausBlur2, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(image1_2, gausBlur12, new OpenCvSharp.Size(11, 11), 1.5);

            Mat imageAvgProduct = gausBlur1.Mul(gausBlur2); //均值乘积
            Mat u1Squre = gausBlur1.Mul(gausBlur1); //各自均值的平方
            Mat u2Squre = gausBlur2.Mul(gausBlur2);

            Mat imageConvariance = new Mat(), imageVariance1 = new Mat(), imageVariance2 = new Mat();
            Mat squreAvg1 = new Mat(), squreAvg2 = new Mat();
            Cv2.GaussianBlur(image1_1, squreAvg1, new OpenCvSharp.Size(11, 11), 1.5); //图像平方的均值
            Cv2.GaussianBlur(image2_2, squreAvg2, new OpenCvSharp.Size(11, 11), 1.5);

            imageConvariance = gausBlur12 - gausBlur1.Mul(gausBlur2);// 计算协方差
            imageVariance1 = squreAvg1 - gausBlur1.Mul(gausBlur1); //计算方差
            imageVariance2 = squreAvg2 - gausBlur2.Mul(gausBlur2);

            var member = ((2 * gausBlur1.Mul(gausBlur2) + C1).Mul(2 * imageConvariance + C2));
            var denominator = ((u1Squre + u2Squre + C1).Mul(imageVariance1 + imageVariance2 + C2));

            Mat ssim = new Mat();
            Cv2.Divide(member, denominator, ssim);

            var sclar = Cv2.Mean(ssim);

            return sclar;  // 变化率，即差异

        }

        public Bitmap MatToBitmap(Mat mat)
        {
            mat.ConvertTo(mat, MatType.CV_8U);
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        }
        public float[] BitmapToFloatArray(Bitmap bmp)
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


        public float[] MatToFloatArray(Mat mat)
        {
            Mat[] rgbChannels = new Mat[3];
            Cv2.Split(mat, out rgbChannels);

            int image_area = mat.Rows * mat.Cols;
            List<float> data = new List<float>();
            for (int c = 0; c < 3; c++)
            {
                Mat floatChannel = new Mat();
                rgbChannels[c].ConvertTo(floatChannel, MatType.CV_32FC1);
                float[] channelData = new float[mat.Rows * mat.Cols];
                Marshal.Copy(floatChannel.Data, channelData, 0, channelData.Length);
                data.AddRange(channelData);
            }

            float[] pdata = data.ToArray();
            return pdata;
        }


        public void GetROI2(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness, out float[] src1, bool isRotate, bool saveImg = false, bool isColor = false, bool isOnline = false)
        {
            try
            {
                sharpness = new double[5];
                sharpness[0] = 0;
                sharpness[1] = 0;
                sharpness[2] = 0;
                sharpness[3] = 0;
                sharpness[4] = 0;
                Mat sourceColor = new Mat();
                Mat source = new Mat();
                if (isColor)
                {
                    sourceColor = new Mat(height, width, MatType.CV_8UC3, data);
                    Cv2.CvtColor(sourceColor, source, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    source = new Mat(height, width, MatType.CV_8UC1, data);
                }
                Mat pro = new Mat();
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchyIndices;
                Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
                if (saveImg)
                {
                    Cv2.ImWrite("GaussianBlur.bmp", pro);
                }
                CLAHE clahe = Cv2.CreateCLAHE(clipLimit: 1.0, tileGridSize: new OpenCvSharp.Size(8, 8));
                clahe.Apply(pro, pro);

                Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
                if (saveImg)
                {
                    Cv2.ImWrite("Otsu.bmp", pro);
                }

                int erodeSize = 41;
                Mat erodeSe = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(erodeSize, erodeSize), new OpenCvSharp.Point(-1, -1));
                Mat Erode = new Mat();
                Cv2.Erode(pro, Erode, erodeSe);
                if (saveImg)
                {
                    Cv2.ImWrite("Erode.bmp", Erode);
                }
                

                int size = 5;
                Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
                Cv2.Dilate(pro, pro, se);
                if (saveImg)
                {
                    Cv2.ImWrite("Dilate.bmp", pro);
                }
                Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                if (saveImg)
                {
                    Mat drawCon = Mat.Zeros(pro.Size(), source.Type());
                    for (int i = 0; i < contours.Length; i++)
                    {
                        Cv2.DrawContours(drawCon, contours, i, new Scalar(255, 255, 255), 2, LineTypes.Link8, hierarchyIndices);
                    }
                    Cv2.ImWrite("FindContours.bmp", drawCon);
                }


                if (contours.Length <= 0)
                {
                    srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                    src1 = null;
                    return;
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
                margnRect.Size.Height = rotatedRect.Size.Height + 50;
                margnRect.Size.Width = rotatedRect.Size.Width + 50;


                Point2f[] dstPt = new Point2f[4];
                dstPt[0] = new Point2f(0, margnRect.Size.Height);
                dstPt[1] = new Point2f(0, 0);
                dstPt[2] = new Point2f(margnRect.Size.Width, 0);
                dstPt[3] = new Point2f(margnRect.Size.Width, margnRect.Size.Height);
                Mat result = new Mat();
                Mat m = Cv2.GetPerspectiveTransform(margnRect.Points(), dstPt);
                if (isColor)
                {
                    Cv2.WarpPerspective(sourceColor, result, m, new OpenCvSharp.Size(margnRect.Size.Width, margnRect.Size.Height));
                }
                else
                {
                    Cv2.WarpPerspective(source, result, m, new OpenCvSharp.Size(margnRect.Size.Width, margnRect.Size.Height));
                }

                if (result.Height > result.Width)
                {
                    Cv2.Transpose(result, result);
                    Cv2.Flip(result, result, FlipMode.Y);
                }

                if (
                    (result.Width > 1950 && result.Width < 2270 && result.Height > 1700 && result.Height < 1900) ||
                    (result.Height > 1950 && result.Height < 2270 && result.Width > 1700 && result.Width < 1900) ||
                     (result.Width > 3000 && result.Width < 3230 && result.Height > 1700 && result.Height < 1900) ||
                    (result.Height > 3000 && result.Height < 3230 && result.Width > 1700 && result.Width < 1900)
                    )
                {
                    Mat imgSobel = new Mat();
                    Cv2.Sobel(result, imgSobel, MatType.CV_16U, 1, 1);
                    if (saveImg)
                    {
                        Cv2.ImWrite("sobel.bmp", imgSobel);
                    }

                    if (imgSobel.Channels() > 1)
                    {
                        Mat imgSobelGray = new Mat();
                        Cv2.CvtColor(imgSobel, imgSobelGray, ColorConversionCodes.BGR2GRAY);
                        Cv2.Threshold(imgSobelGray, imgSobel, 0, 255, ThresholdTypes.Otsu);
                    }
                    else
                    {
                        Cv2.Threshold(imgSobel, imgSobel, 0, 255, ThresholdTypes.Otsu);
                    }

                    if (saveImg)
                    {
                        Cv2.ImWrite("sobelOtsu.bmp", imgSobel);
                    }
                    Console.WriteLine("result width: {0}, result height: {1}", result.Width, result.Height);
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

                    // judge blurry
                    if (IsOpenDetect)
                    {
                        double minSharpness = Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]);
                        if (minSharpness < 0.8 && (sharpness[1] > 0 || sharpness[2] > 0 || sharpness[3] > 0 || sharpness[4] > 0))
                        {
                            IsBlurry = 1;
                            srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                            src1 = null;
                            return;
                        }
                    }

                    Mat dst = new Mat();
                    Cv2.Merge(new[] { result, result, result }, dst);
                    float srcWidth = dst.Cols;
                    float srcHeight = dst.Rows;
                    Mat max_image_nor_resize = dst.Resize(new OpenCvSharp.Size(1280, 1280), interpolation: InterpolationFlags.Cubic);
                    src1 = MatToFloat(max_image_nor_resize);
                }
                else
                {
                    srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                    src1 = null;
                    return;
                }

                // check backside
                if (IsOpenDetect)
                {
                    OpenCvSharp.Point[][] contoursKeep;
                    Cv2.FindContours(Erode, out contoursKeep, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                    double maxConcourArea = 0;
                    for (int i = 0; i < contoursKeep.Length; i++)
                    {
                        double area = Cv2.ContourArea(contoursKeep[i]);
                        if (area > maxConcourArea)
                        {
                            maxConcourArea = area;
                        }
                    }

                    if (saveImg)
                    {
                        int k = 0;
                        Mat blackImage = new Mat(new OpenCvSharp.Size(3200, 2800), MatType.CV_8UC3, Scalar.Black);

                        OpenCvSharp.Point center;
                        double fontScale = 0.5;
                        Scalar textcolor = new Scalar(255, 255, 255);
                        Scalar contourcolor = new Scalar(100, 100, 100);
                        Scalar keycontourcolor = new Scalar(0, 0, 255);

                        for (int i = 0; i < contoursKeep.Length; i++)
                        {
                            double len = Cv2.ArcLength(contoursKeep[i], true);
                            double area = Cv2.ContourArea(contoursKeep[i]);
                            if (area > 20000)
                            {
                                k++;
                                if (len > 2000 && len < 3800 && area > 230000 && area < 330000)
                                {
                                    Cv2.DrawContours(blackImage, contoursKeep, i, keycontourcolor, 2);
                                }
                                else
                                {
                                    Cv2.DrawContours(blackImage, contoursKeep, i, contourcolor, 2);
                                }
                                Moments moments = Cv2.Moments(contoursKeep[i]);
                                center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));

                                // Draw text
                                Cv2.PutText(blackImage, "Area: " + area.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y), HersheyFonts.HersheySimplex, fontScale, textcolor);
                                Cv2.PutText(blackImage, "Len: " + len.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y + 30), HersheyFonts.HersheySimplex, fontScale, textcolor);
                            }
                        }
                        Cv2.ImWrite("ErodeWithContours.bmp", blackImage);
                    }
                    if (maxConcourArea > 2400000)
                    {
                        backside = 1;
                        srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                        src1 = null;
                        return;
                    }
                }

                // check slider ori
                if (IsOpenDetect)
                {
                    bool oritentionSlider = JudgeSliderOritention(result, saveImg);
                    if (!oritentionSlider)
                    {
                        orientation = 1;
                        srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                        src1 = null;
                        return;
                    }
                }

                // judge type
                if (IsDetectHeadType && isOnline)
                {
                    string sliderType = JudgeSliderType(result, saveImg);
                    if (sliderType != HeadType && sliderType != "None")
                    {
                        IsTypeWrong = 1;
                        srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                        src1 = null;
                        return;
                    }
                }

                if (saveImg)
                {
                    Cv2.ImWrite("sliderImage.bmp", result);
                }
                // Rotate image
                if (isRotate)
                {
                    Mat rotateRet = new Mat();
                    Cv2.Rotate(result, rotateRet, RotateFlags.Rotate90Clockwise);
                    srcImg = MatToBitmap(rotateRet);//source
                }
                else
                {
                    srcImg = MatToBitmap(result);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Find Slider error:" + e.ToString());
            }
        }

        public void GetROI22(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness, out float[] src1, bool isRotate, bool saveImg = false, bool isColor = false, bool isOnline = false)
        {
            try
            {
                sharpness = new double[5];
                sharpness[0] = 0;
                sharpness[1] = 0;
                sharpness[2] = 0;
                sharpness[3] = 0;
                sharpness[4] = 0;
                Mat sourceColor = new Mat();
                Mat source = new Mat();
                if (isColor)
                {
                    sourceColor = new Mat(height, width, MatType.CV_8UC3, data);
                    Cv2.CvtColor(sourceColor, source, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    source = new Mat(height, width, MatType.CV_8UC1, data);
                }
                Mat pro = new Mat();

                Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
                if (saveImg)
                {
                    Cv2.ImWrite("GaussianBlur.bmp", pro);
                }

                Mat laplacian = new Mat();
                Cv2.Laplacian(pro, laplacian, MatType.CV_64F);
                Mat laplacianAbs = new Mat();
                Cv2.ConvertScaleAbs(laplacian, laplacianAbs);

                Mat sharpRegions = new Mat();
                Cv2.Threshold(laplacianAbs, sharpRegions, 4, 255, ThresholdTypes.Binary);

                if (saveImg)
                {
                    Cv2.ImWrite("laplacianAbs.bmp", laplacianAbs);
                    Cv2.ImWrite("sharpRegions.bmp", sharpRegions);
                }

                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                Cv2.MorphologyEx(sharpRegions, sharpRegions, MorphTypes.Close, kernel);

                Mat maskedGray = new Mat();
                Cv2.BitwiseAnd(pro, sharpRegions, maskedGray);

                Mat edges = new Mat();
                Cv2.Canny(maskedGray, edges, 50, 150);


                kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(15, 10));
                Cv2.MorphologyEx(edges, edges, MorphTypes.Dilate, kernel);

                if (saveImg)
                {
                    Cv2.ImWrite("edges.bmp", edges);
                }


                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                RotatedRect bestRect = new RotatedRect();
                double maxArea = 0;
                foreach (var contour in contours)
                {
                    double area = Cv2.ContourArea(contour);
                    if (area < 1000)
                        continue;
                    {
                        RotatedRect rect = Cv2.MinAreaRect(contour);
                        double rectArea = rect.Size.Width * rect.Size.Height;
                        if (rectArea > maxArea)
                        {
                            maxArea = rectArea;
                            bestRect = rect;
                        }
                    }
                }

                bestRect.Size.Height = bestRect.Size.Height + 50;
                bestRect.Size.Width = bestRect.Size.Width + 50;

                if (maxArea == 0)
                {
                    srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                    src1 = null;
                    return;
                }

                Point2f[] boxPoints = bestRect.Points();
                Point2f[] sortedPoints = SortPoints(boxPoints);

                float widthA = Distance(sortedPoints[0], sortedPoints[1]);
                float widthB = Distance(sortedPoints[2], sortedPoints[3]);
                float maxWidth = Math.Max(widthA, widthB);

                float heightA = Distance(sortedPoints[0], sortedPoints[3]);
                float heightB = Distance(sortedPoints[1], sortedPoints[2]);
                float maxHeight = Math.Max(heightA, heightB);

                Point2f[] dstPoints = new Point2f[]
                {
                    new Point2f(0, 0),
                    new Point2f(maxWidth - 1, 0),
                    new Point2f(maxWidth - 1, maxHeight - 1),
                    new Point2f(0, maxHeight - 1)
                };

                Mat M = Cv2.GetPerspectiveTransform(sortedPoints, dstPoints);
                Mat warped = new Mat();

                if (isColor)
                {
                    Cv2.WarpPerspective(sourceColor, warped, M, new OpenCvSharp.Size((int)maxWidth, (int)maxHeight));
                }
                else
                {
                    Cv2.WarpPerspective(source, warped, M, new OpenCvSharp.Size((int)maxWidth, (int)maxHeight));
                }

                if (warped.Height > warped.Width)
                {
                    Cv2.Transpose(warped, warped);
                    Cv2.Flip(warped, warped, FlipMode.Y);
                }

                if (saveImg)
                {
                    Cv2.ImWrite("warped.bmp", warped);
                }

                if (
                    (warped.Width > 1950 && warped.Width < 2270 && warped.Height > 1700 && warped.Height < 1900) ||
                    (warped.Height > 1950 && warped.Height < 2270 && warped.Width > 1700 && warped.Width < 1900) ||
                     (warped.Width > 3000 && warped.Width < 3230 && warped.Height > 1700 && warped.Height < 1900) ||
                    (warped.Height > 3000 && warped.Height < 3230 && warped.Width > 1700 && warped.Width < 1900)
                    )
                {
                    Mat imgSobel = new Mat();
                    Cv2.Sobel(warped, imgSobel, MatType.CV_16U, 1, 1);
                    if (imgSobel.Channels() > 1)
                    {
                        Mat imgSobelGray = new Mat();
                        Cv2.CvtColor(imgSobel, imgSobelGray, ColorConversionCodes.BGR2GRAY);
                        Cv2.Threshold(imgSobelGray, imgSobel, 0, 255, ThresholdTypes.Otsu);
                    }
                    else
                    {
                        Cv2.Threshold(imgSobel, imgSobel, 0, 255, ThresholdTypes.Otsu);
                    }
                    // Console.WriteLine("result width: {0}, result height: {1}", warped.Width, warped.Height);
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

                    // judge blurry
                    if (IsOpenDetect)
                    {
                        double minSharpness = Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]);
                        if (minSharpness < 0.8 && (sharpness[1] > 0 || sharpness[2] > 0 || sharpness[3] > 0 || sharpness[4] > 0))
                        {
                            IsBlurry = 1;
                            srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                            src1 = null;
                            return;
                        }
                    }
                }
                else
                {
                    srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                    src1 = null;
                    return;
                }

                // judge backside
                if (IsOpenDetect)
                {
                    double maxContourArea = JudgeBackside(warped, saveImg);
                    if (maxContourArea > 2400000)
                    {
                        backside = 1;
                        srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                        src1 = null;
                        return;
                    }
                }

                // judge oritention
                if (IsOpenDetect)
                {
                    bool oritentionSlider = JudgeSliderOritention(warped, saveImg);
                    if (!oritentionSlider)
                    {
                        orientation = 1;
                        srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                        src1 = null;
                        return;
                    }
                }

                // judge type
                if (IsDetectHeadType && isOnline)
                {
                    string sliderType = JudgeSliderType(warped, saveImg);
                    if (sliderType != HeadType && sliderType != "None")
                    {
                        IsTypeWrong = 1;
                        srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                        src1 = null;
                        return;
                    }
                }

                // Rotate image
                if (isRotate)
                {
                    Mat rotateRet = new Mat();
                    Cv2.Rotate(warped, rotateRet, RotateFlags.Rotate90Clockwise);
                    srcImg = MatToBitmap(rotateRet);//source
                }
                else
                {
                    srcImg = MatToBitmap(warped);
                }

                Mat warped3Channel = new Mat();
                // Legacy: read "Site" from app.config via ConfigurationManager; here use SliderRoiExtractor.Site.
                if (SliderRoiExtractor.Site == "THO")
                {
                    Cv2.Merge(new[] { warped, warped, warped }, warped3Channel);
                }
                else if (SliderRoiExtractor.Site == "PHO")
                {
                    Mat rotateRet = new Mat();
                    Cv2.Rotate(warped, rotateRet, RotateFlags.Rotate90Clockwise);
                    Cv2.Merge(new[] { rotateRet, rotateRet, rotateRet }, warped3Channel);
                }
                else
                {
                    Cv2.Merge(new[] { warped, warped, warped }, warped3Channel);
                }

                Mat floatSrc = warped3Channel.Resize(new OpenCvSharp.Size(1280, 1280), interpolation: InterpolationFlags.Cubic);

                src1 = MatToFloat(floatSrc);
            }
            catch (Exception e)
            {
                throw new Exception("Find Slider error:" + e.ToString());
            }
        }

        static Point2f[] SortPoints(Point2f[] pts)
        {
            // 按y坐标排序
            Array.Sort(pts, (a, b) => a.Y.CompareTo(b.Y));
            Point2f[] top = new Point2f[2] { pts[0], pts[1] };
            Point2f[] bottom = new Point2f[2] { pts[2], pts[3] };

            // 对顶部两个点按x排序
            if (top[0].X > top[1].X)
            {
                var temp = top[0];
                top[0] = top[1];
                top[1] = temp;
            }

            // 对底部两个点按x排序
            if (bottom[0].X > bottom[1].X)
            {
                var temp = bottom[0];
                bottom[0] = bottom[1];
                bottom[1] = temp;
            }

            // 返回排序顺序：左上、右上、右下、左下
            return new Point2f[] { top[0], top[1], bottom[1], bottom[0] };
        }

        static float Distance(Point2f a, Point2f b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }


        double JudgeBackside(Mat src, bool saveImg)
        {

            Mat pro = new Mat();
            // Cv2.BilateralFilter(src, pro, 15, 80, 5);
            Cv2.Threshold(src, pro, 0, 255, ThresholdTypes.Otsu);
            if (saveImg)
            {
                Cv2.ImWrite("Otsu.bmp", pro);
            }
            //int coreSize = 3;
            // Mat coreSe = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(coreSize, coreSize), new OpenCvSharp.Point(-1, -1));
            //Cv2.MorphologyEx(pro, pro, MorphTypes.Close, coreSe);
            int erodeSize = 15;
            Mat coreSe = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(erodeSize, erodeSize), new OpenCvSharp.Point(-1, -1));
            Cv2.MorphologyEx(pro, pro, MorphTypes.Erode, coreSe);
            //int openSize = 15;
            //coreSe = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(openSize, openSize), new OpenCvSharp.Point(-1, -1));
            //Cv2.MorphologyEx(pro, pro, MorphTypes.Dilate, coreSe);

            if (saveImg)
            {
                Cv2.ImWrite("Erode.bmp", pro);
            }

            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            // calculate connected area
            Cv2.ConnectedComponentsWithStats(
                    pro,
                    labels,
                    stats,
                    centroids,
                    PixelConnectivity.Connectivity8
                );
            double maxConcourArea = 0;
            for (int i = 1; i < stats.Rows; i++)
            {
                int area = stats.Get<int>(i, (int)ConnectedComponentsTypes.Area);
                // nosie
                if (area < 50) continue;
                if (area > maxConcourArea ) maxConcourArea = area;
            }

            /*
            HierarchyIndex[] hierarchyIndices;
            OpenCvSharp.Point[][] contoursKeep;
            Cv2.FindContours(pro, out contoursKeep, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            double maxConcourArea = 0;
            for (int i = 0; i < contoursKeep.Length; i++)
            {
                double area = Cv2.ContourArea(contoursKeep[i]);
                if (area > maxConcourArea)
                {
                    maxConcourArea = area;
                }
            }
            if (saveImg)
            {
                int k = 0;
                Mat blackImage = new Mat(new OpenCvSharp.Size(src.Width, src.Height), MatType.CV_8UC3, Scalar.Black);

                OpenCvSharp.Point center;
                double fontScale = 0.5;
                Scalar textcolor = new Scalar(255, 255, 255);
                Scalar contourcolor = new Scalar(100, 100, 100);
                Scalar keycontourcolor = new Scalar(0, 0, 255);

                for (int i = 0; i < contoursKeep.Length; i++)
                {
                    double len = Cv2.ArcLength(contoursKeep[i], true);
                    double area = Cv2.ContourArea(contoursKeep[i]);
                    if (area > 20000)
                    {
                        k++;
                        if (len > 2000 && len < 3800 && area > 230000 && area < 330000)
                        {
                            Cv2.DrawContours(blackImage, contoursKeep, i, keycontourcolor, 2);
                        }
                        else
                        {
                            Cv2.DrawContours(blackImage, contoursKeep, i, contourcolor, 2);
                        }
                        Moments moments = Cv2.Moments(contoursKeep[i]);
                        center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));

                        // Draw text
                        Cv2.PutText(blackImage, "Area: " + area.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y), HersheyFonts.HersheySimplex, fontScale, textcolor);
                        Cv2.PutText(blackImage, "Len: " + len.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y + 30), HersheyFonts.HersheySimplex, fontScale, textcolor);
                    }
                }
                Cv2.ImWrite("ErodeWithContours.bmp", blackImage);
            }
            */

            return maxConcourArea;

        }

        bool JudgeSliderOritention(Mat src, bool saveImg)
        {
            Rect leftArea = new Rect(0, 0, 200, src.Height);
            Mat LeftAreaImg = new Mat(src, leftArea);
            if (saveImg)
            {
                Cv2.ImWrite("left.bmp", LeftAreaImg);
            }
            Mat leftOtsu = new Mat();
            Cv2.GaussianBlur(LeftAreaImg, LeftAreaImg, new OpenCvSharp.Size(3, 3), 1.5);
            if (LeftAreaImg.Channels() > 1)
            {
                Mat LeftAreaImgGray = new Mat();
                Cv2.CvtColor(LeftAreaImg, LeftAreaImgGray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(LeftAreaImgGray, leftOtsu, 0, 255, ThresholdTypes.Otsu);
            }
            else
            {
                Cv2.Threshold(LeftAreaImg, leftOtsu, 0, 255, ThresholdTypes.Otsu);
            }
            InputArray kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5), new OpenCvSharp.Point(-1, -1));
            Cv2.MorphologyEx(leftOtsu, leftOtsu, MorphTypes.Open, kernel, new OpenCvSharp.Point(-1, -1), 1, BorderTypes.Constant, Scalar.Gold);

            HierarchyIndex[] hierarchyIndices;
            OpenCvSharp.Point[][] contours;
            Cv2.FindContours(leftOtsu, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            int leftLen = contours.Length;
            if (saveImg)
            {
                Cv2.ImWrite("leftOtsu.bmp", leftOtsu);
            }
            // double leftMean= Cv2.Mean(LeftAreaImg)[0];

            Rect rightArea = new Rect(src.Width - 200, 0, 200, src.Height);
            Mat RightAreaImg = new Mat(src, rightArea);
            if (saveImg)
            {
                Cv2.ImWrite("right.bmp", RightAreaImg);
            }
            Mat rightOtsu = new Mat();
            Cv2.GaussianBlur(RightAreaImg, RightAreaImg, new OpenCvSharp.Size(3, 3), 1.5);

            if (RightAreaImg.Channels() > 1)
            {
                Mat RightAreaImgGray = new Mat();
                Cv2.CvtColor(RightAreaImg, RightAreaImgGray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(RightAreaImgGray, rightOtsu, 0, 255, ThresholdTypes.Otsu);
            }
            else
            {
                Cv2.Threshold(RightAreaImg, rightOtsu, 0, 255, ThresholdTypes.Otsu);
            }
            Cv2.MorphologyEx(rightOtsu, rightOtsu, MorphTypes.Open, kernel, new OpenCvSharp.Point(-1, -1), 1, BorderTypes.Constant, Scalar.Gold);

            Cv2.FindContours(rightOtsu, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            int rightLen = contours.Length;
            if (saveImg)
            {
                Cv2.ImWrite("rightOtsu.bmp", rightOtsu);
            }

            return leftLen > rightLen ? true : false;
        }

        string JudgeSliderType(Mat src, bool saveImg)
        {
            Rect middleArea = new Rect((int)(src.Width * 0.234) - 60, (int)(src.Height / 2) - 60, 120, 120);
            Mat middleAreaImg = new Mat(src, middleArea);
            if (saveImg)
            {
                Cv2.ImWrite("middleAreaImg.bmp", middleAreaImg);
            }
            Mat middleOtsu = new Mat();

            if (middleAreaImg.Channels() > 1)
            {
                Mat middleAreaImgGray = new Mat();
                Cv2.CvtColor(middleAreaImg, middleAreaImgGray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(middleAreaImgGray, middleOtsu, 0, 255, ThresholdTypes.Otsu);
            }
            else
            {
                //Cv2.Threshold(middleAreaImg, middleOtsu, 83, 255, ThresholdTypes.Binary); // Cv2.Threshold(middleAreaImg, middleOtsu, 0, 255, ThresholdTypes.Otsu);
                Mat bilateralImg = new Mat();
                Cv2.BilateralFilter(middleAreaImg, bilateralImg, 15, 150, 5);

                // Cv2.GaussianBlur(middleAreaImg, bilateralImg, new OpenCvSharp.Size(3, 3), 1.5);
                //double alpha = 0.8;
                //double beta = -50;
                //Mat brightImage = new Mat();
                //Cv2.ConvertScaleAbs(bilateralImg, brightImage, alpha, beta);
                Cv2.Threshold(bilateralImg, middleOtsu, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.BinaryInv); // | ThresholdTypes.BinaryInv
            }
            if (saveImg)
            {
                Cv2.ImWrite("middleAreaOtsu.bmp", middleOtsu);
            }

            HierarchyIndex[] hierarchyIndices;
            OpenCvSharp.Point[][] contours;
            Cv2.FindContours(middleOtsu, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            double midmaxConcourArea = 0;
            int maxConcourIndex = -1;


            OpenCvSharp.Point2f momentcenter = new Point2f();
            OpenCvSharp.Point2f rectcenter = new Point2f();

            bool canJudge = false;
            if (contours.Length > 0)
            {
                for (int i = 0; i < contours.Length; i++)
                {
                    double area = Cv2.ContourArea(contours[i]);
                    if (area < 3600 && area > 800)
                    {
                        if (area > midmaxConcourArea)
                        {
                            midmaxConcourArea = area;
                            maxConcourIndex = i;
                        }
                    }
                }
                if (saveImg)
                {
                    int k = 0;
                    Mat blackImage = new Mat(new OpenCvSharp.Size(middleAreaImg.Width, middleAreaImg.Height), MatType.CV_8UC3, Scalar.Black);

                    OpenCvSharp.Point center;
                    OpenCvSharp.Point2f center1;
                    double fontScale = 0.5;
                    Scalar textcolor = new Scalar(255, 255, 255);
                    Scalar contourcolor = new Scalar(0, 0, 255);
                    Scalar contourcolor1 = new Scalar(0, 255, 0);

                    for (int i = 0; i < contours.Length; i++)
                    {
                        double area = Cv2.ContourArea(contours[i]);
                        if (area < 3600 && area > 800)
                        {
                            k++;
                            contours[i] = Cv2.ConvexHull(contours[i]);
                            Cv2.DrawContours(blackImage, contours, i, contourcolor, 1);
                            Moments moments = Cv2.Moments(contours[i]);
                            center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
                            center1 = Cv2.MinAreaRect(contours[i]).Center;
                            // Draw text
                            Cv2.Circle(blackImage, center, 2, contourcolor);
                            Cv2.Circle(blackImage, new OpenCvSharp.Point(center1.X, center1.Y), 2, contourcolor1);
                        }
                    }
                    Cv2.ImShow("ErodeWithContoursMid", blackImage);
                    Cv2.ImWrite("ErodeWithContoursMid.bmp", blackImage);
                }
                if (maxConcourIndex >= 0)
                {

                    OpenCvSharp.Point[] hull = Cv2.ConvexHull(contours[maxConcourIndex]);
                    Rect tmp = Cv2.BoundingRect(hull);

                    if (tmp.Height < 75) // 53
                    {
                        Moments minmoments = Cv2.Moments(hull);
                        RotatedRect midrotatedRect = Cv2.MinAreaRect(hull);
                        momentcenter = new OpenCvSharp.Point((minmoments.M10 / minmoments.M00), (minmoments.M01 / minmoments.M00));
                        rectcenter = midrotatedRect.Center;
                        canJudge = true;
                    }
                }
            }

            if (canJudge)
                return rectcenter.Y <= momentcenter.Y ? "A" : "B";
            else
            {
                Cv2.Threshold(middleOtsu, middleOtsu, 0, 255, ThresholdTypes.BinaryInv);
                Cv2.FindContours(middleOtsu, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                midmaxConcourArea = 0;
                maxConcourIndex = -1;
                if (contours.Length > 0)
                {
                    for (int i = 0; i < contours.Length; i++)
                    {
                        double area = Cv2.ContourArea(contours[i]);
                        if (area < 3600 && area > 400)
                        {
                            if (area > midmaxConcourArea)
                            {
                                midmaxConcourArea = area;
                                maxConcourIndex = i;
                            }
                        }
                    }
                    if (saveImg)
                    {
                        int k = 0;
                        Mat blackImage = new Mat(new OpenCvSharp.Size(middleAreaImg.Width, middleAreaImg.Height), MatType.CV_8UC3, Scalar.Black);

                        OpenCvSharp.Point center;
                        OpenCvSharp.Point2f center1;
                        Scalar textcolor = new Scalar(255, 255, 255);
                        Scalar contourcolor = new Scalar(0, 0, 255);
                        Scalar contourcolor1 = new Scalar(0, 255, 0);

                        for (int i = 0; i < contours.Length; i++)
                        {
                            double len = Cv2.ArcLength(contours[i], true);
                            double area = Cv2.ContourArea(contours[i]);
                            if (area < 3600 && area > 400)
                            {
                                k++;
                                Cv2.DrawContours(blackImage, contours, i, contourcolor, 1);
                                Moments moments = Cv2.Moments(contours[i]);
                                center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
                                center1 = Cv2.MinAreaRect(contours[i]).Center;
                                // Draw text
                                Cv2.Circle(blackImage, center, 2, contourcolor);
                                Cv2.Circle(blackImage, new OpenCvSharp.Point(center1.X, center1.Y), 2, contourcolor1);
                            }
                        }
                        Cv2.ImShow("ErodeWithContoursMid", blackImage);
                        Cv2.ImWrite("ErodeWithContoursMid.bmp", blackImage);
                    }
                    if (maxConcourIndex >= 0)
                    {
                        Rect tmp = Cv2.BoundingRect(contours[maxConcourIndex]);
                        if (tmp.Height < 80)
                        {
                            Moments minmoments = Cv2.Moments(contours[maxConcourIndex]);
                            RotatedRect midrotatedRect = Cv2.MinAreaRect(contours[maxConcourIndex]);
                            momentcenter = new OpenCvSharp.Point((minmoments.M10 / minmoments.M00), (minmoments.M01 / minmoments.M00));
                            rectcenter = midrotatedRect.Center;
                            canJudge = true;
                        }
                    }

                    if (canJudge)
                        return rectcenter.Y < momentcenter.Y ? "A" : "B";
                    else
                        return "None"; // can't judge Type
                }
                else
                    return "None"; // can't judge Type



            }
        }


        public void GetROI3(byte[] data, int height, int width, out Bitmap srcImg)
        {
            try
            {
                Mat sourceColor = new Mat();
                sourceColor = new Mat(height, width, MatType.CV_8UC3, data);
                srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sourceColor);
            }
            catch (Exception e)
            {
                srcImg = null;
                throw new Exception(e.ToString());
            }

        }

        public void GetRowBarDepoEdge(byte[] data, int height, int width, bool olg, bool saveImg, out Bitmap srcImg, out double olgSharpness)
        {
            try
            {
                olgSharpness = 0;
                Mat sourceColor = new Mat();
                sourceColor = new Mat(height, width, MatType.CV_8UC3, data);
                // srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sourceColor);
                Mat pro = new Mat();
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchyIndices;

                Cv2.GaussianBlur(sourceColor, pro, new OpenCvSharp.Size(7, 7), 1.5);
                //Cv2.Blur(pro, pro, new OpenCvSharp.Size(10, 10));
                Cv2.CvtColor(pro, pro, ColorConversionCodes.BGR2GRAY);
                double rettmp = Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);
                Cv2.Blur(pro, pro, new OpenCvSharp.Size(100, 10));
                if (saveImg)
                {
                    Cv2.ImWrite("Threshold.bmp", pro);
                }

                int size = 9;
                Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
                Cv2.Dilate(pro, pro, se);

                if (saveImg)
                {
                    Cv2.ImWrite("Dilate.bmp", pro);
                }

                Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                int index = -1;
                if (contours != null && contours.Length > 0)
                {
                    index = 0;
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
                }

                if (saveImg && contours != null)
                {
                    int k = 0;
                    Mat blackImage = new Mat(new OpenCvSharp.Size(sourceColor.Width, sourceColor.Height), MatType.CV_8UC3, Scalar.Black);

                    OpenCvSharp.Point center;
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
                            k++;
                            if (len > 2000 && len < 3800 && area > 10000 && area < 330000)
                            {
                                Cv2.DrawContours(blackImage, contours, i, keycontourcolor, 2);
                            }
                            else
                            {
                                Cv2.DrawContours(blackImage, contours, i, contourcolor, 2);
                            }
                            Moments moments = Cv2.Moments(contours[i]);
                            center = new OpenCvSharp.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));

                            // Draw text
                            Cv2.PutText(blackImage, "Area: " + area.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y), HersheyFonts.HersheySimplex, fontScale, textcolor);
                            Cv2.PutText(blackImage, "Len: " + len.ToString(), new OpenCvSharp.Point(center.X - 50, center.Y + 30), HersheyFonts.HersheySimplex, fontScale, textcolor);
                        }
                    }
                    Cv2.ImWrite("ErodeWithContours.bmp", blackImage);
                }

                if (index >= 0)
                {
                    RotatedRect rotatedRect = Cv2.MinAreaRect(contours[index]);
                    RotatedRect alignRect = new RotatedRect();
                    if (rotatedRect.Angle > 45)
                    {
                        Size2f newSize = new Size2f(rotatedRect.Size.Height, rotatedRect.Size.Width);
                        float newAngle = rotatedRect.Angle - 90;
                        if (newAngle > 180) newAngle -= 360;
                        if (newAngle <= 180) newAngle += 360;

                        alignRect = new RotatedRect(rotatedRect.Center, newSize, newAngle);
                    }
                    else
                    {
                        alignRect = rotatedRect;
                    }


                    if (alignRect.Size.Height < 300 || alignRect.Size.Height > 1000)
                    {
                        srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sourceColor);
                        return;
                    }
                    else
                    {
                        Point2f[] corners = alignRect.Points();
                        int pad = 30;
                        float minX = corners.Min(p => p.X) - pad;
                        float maxX = corners.Max(p => p.X) + pad;
                        float minY = corners.Min(p => p.Y) - pad;
                        float maxY = corners.Max(p => p.Y) + pad;

                        int x0 = Math.Max((int)Math.Floor(minX), 0);
                        int y0 = Math.Max((int)Math.Floor(minY), 0);
                        int x1 = Math.Min((int)Math.Ceiling(maxX), sourceColor.Width);
                        int y1 = Math.Min((int)Math.Ceiling(maxY), sourceColor.Height);

                        int w = x1 - x0;
                        int h = y1 - y0;

                        Rect roiSrc = new Rect(x0, y0, w, h);
                        Mat sub = new Mat(sourceColor, roiSrc);

                        Point2f centerSub = new Point2f(alignRect.Center.X - x0, alignRect.Center.Y - y0);
                        Mat M = Cv2.GetRotationMatrix2D(centerSub, alignRect.Angle, 1.0);

                        Mat rotatedSub = new Mat();
                        Cv2.WarpAffine(sub, rotatedSub, M, new OpenCvSharp.Size(w, h),
                                       InterpolationFlags.Linear,
                                       BorderTypes.Constant, Scalar.Black);

                        Size2f newSize = new Size2f(alignRect.Size.Width + 2 * pad, alignRect.Size.Height + 2 * pad);
                        int finalW = (int)Math.Round(newSize.Width);
                        int finalH = (int)Math.Round(newSize.Height);
                        int fx0 = (int)Math.Round(centerSub.X - finalW / 2.0);
                        int fy0 = (int)Math.Round(centerSub.Y - finalH / 2.0);

                        Rect finalRoi = new Rect(fx0, fy0, finalW, finalH);
                        Rect bound = new Rect(0, 0, rotatedSub.Width, rotatedSub.Height);
                        Rect safe = finalRoi & bound;
                        if (safe.Width <= 0 || safe.Height <= 0)
                        {
                            srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sourceColor);
                            return;
                        }
                        if (olg)
                        {
                            safe.X = rotatedSub.Width / 2 - 90;
                            safe.Width = 160;
                            Mat olgCrop = new Mat(rotatedSub, safe);
                            olgSharpness = HWKUltra.Vision.Algorithms.Focus.TenengradGradient.GetColor(olgCrop);
                            srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(olgCrop);
                        }
                        else
                            srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(new Mat(rotatedSub, safe));
                    }
                }
                else
                {
                    srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sourceColor);
                    return;
                }
            }
            catch (Exception e)
            {
                srcImg = null;
                throw new Exception(e.ToString());
            }
        }

        public static float[] MatToFloat(Mat data)
        {
            Mat[] rgbChannels = new Mat[3];
            Cv2.Split(data, out rgbChannels);

            int image_area = data.Height * data.Width;
            List<float> fdata = new List<float>();
            for (int i = 0; i < rgbChannels.Length; i++)
            {
                Mat floatChannel = new Mat();
                rgbChannels[i].ConvertTo(floatChannel, MatType.CV_32FC1, 1.0f / 255.0f, 0);
                float[] channelData = new float[data.Width * data.Height];
                Marshal.Copy(floatChannel.Data, channelData, 0, channelData.Length);
                fdata.AddRange(channelData);
            }
            float[] pdata = fdata.ToArray();
            return pdata;
        }

        public void Get50XPoletipROI(byte[] data, int height, int width, out Bitmap srcImg, out double[] sharpness, bool isColor = false, bool saveImg = false, bool saveRaw = false, string rawpath = "", string rawName = "")
        {
            sharpness = new double[5];
            sharpness[0] = 0;
            sharpness[1] = 0;
            sharpness[2] = 0;
            sharpness[3] = 0;
            sharpness[4] = 0;
            Point3D ret = new Point3D(0, 0, 0);
            double scale = 0.5;
            try
            {
                Mat source = new Mat();
                Mat sourceColor = new Mat();
                if (isColor)
                {
                    sourceColor = new Mat(height, width, MatType.CV_8UC3, data);
                    if (saveRaw)
                    {
                        sourceColor.SaveImage(rawpath);
                    }
                    Cv2.Resize(sourceColor, source, OpenCvSharp.Size.Zero, scale, scale);
                    Cv2.CvtColor(source, source, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    throw new Exception("Get50XPoletipROI not support gray input!");
                }
                Mat pro = new Mat();
                Cv2.Blur(source, pro, new OpenCvSharp.Size(13, 13));
                Cv2.Threshold(pro, pro, 200, 255, ThresholdTypes.Binary);
                InputArray kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21, 21), new OpenCvSharp.Point(-1, -1));
                Cv2.MorphologyEx(pro, pro, MorphTypes.Close, kernel, new OpenCvSharp.Point(-1, -1), 1, BorderTypes.Constant, Scalar.Gold);
                // Cv2.MorphologyEx(pro, pro, MorphTypes.Close, kernel, new OpenCvSharp.Point(-1, -1), 1, BorderTypes.Constant, Scalar.Gold);
                if (saveImg)
                {
                    pro.ImWrite(rawName + "_binary.jpg");
                }

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(pro, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                RotatedRect bestRect = new RotatedRect();
                double maxArea = 0;
                foreach (var contour in contours)
                {
                    double area = Cv2.ContourArea(contour);
                    if (area < 7000)
                        continue;
                    {
                        RotatedRect rect = Cv2.MinAreaRect(contour);
                        double rectArea = rect.Size.Width * rect.Size.Height;
                        if (rectArea > maxArea)
                        {
                            maxArea = rectArea;
                            bestRect = rect;
                        }
                    }
                }
                bestRect = NormalizeRotatedRect(bestRect);

                /*
                if (maxArea == 0)
                {
                    if (historyPoletipResult.SizeX == 0)
                    {
                        srcImg = isColor ? MatToBitmap(sourceColor) : MatToBitmap(source);
                        return;
                    }
                    else
                    {
                        // load history rotate
                        bestRect = new RotatedRect(new Point2f(historyPoletipResult.CenterX, historyPoletipResult.CenterY),
                            new Size2f(historyPoletipResult.SizeX, historyPoletipResult.SizeY),
                            historyPoletipResult.Angle);
                    }
                }

                // update history
                historyPoletipResult = new HRotatedRect(bestRect.Center.X,
                    bestRect.Center.Y,
                    bestRect.Size.Width,
                    bestRect.Size.Height,
                    bestRect.Angle);
                */
                bestRect.Center.X = (float)(bestRect.Center.X / scale);
                bestRect.Center.Y = (float)(bestRect.Center.Y / scale);
                bestRect.Size.Height = 1200; //bestRect.Size.Height + 50;
                bestRect.Size.Width = 1400; //bestRect.Size.Width + 50;

                Point2f[] boxPoints = bestRect.Points();
                Point2f[] sortedPoints = SortPoints(boxPoints);

                float widthA = Distance(sortedPoints[0], sortedPoints[1]);
                float widthB = Distance(sortedPoints[2], sortedPoints[3]);
                float maxWidth = Math.Max(widthA, widthB);

                float heightA = Distance(sortedPoints[0], sortedPoints[3]);
                float heightB = Distance(sortedPoints[1], sortedPoints[2]);
                float maxHeight = Math.Max(heightA, heightB);

                Point2f[] dstPoints = new Point2f[]
                {
                    new Point2f(0, 0),
                    new Point2f(maxWidth - 1, 0),
                    new Point2f(maxWidth - 1, maxHeight - 1),
                    new Point2f(0, maxHeight - 1)
                };

                Mat M = Cv2.GetPerspectiveTransform(sortedPoints, dstPoints);
                Mat warped = new Mat();

                if (isColor)
                {
                    Cv2.WarpPerspective(sourceColor, warped, M, new OpenCvSharp.Size((int)maxWidth, (int)maxHeight));
                }
                else
                {
                    Cv2.WarpPerspective(source, warped, M, new OpenCvSharp.Size((int)maxWidth, (int)maxHeight));
                }

                if (warped.Height > warped.Width)
                {
                    Cv2.Transpose(warped, warped);
                    Cv2.Flip(warped, warped, FlipMode.Y);
                }
                Mat gray = new Mat();
                if (warped.Channels() == 3 || warped.Channels() == 4)
                {
                    Cv2.CvtColor(warped, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    warped.CopyTo(gray);
                }


                Mat imgSobelx = new Mat();
                Mat imgSobely = new Mat();
                Cv2.Sobel(gray, imgSobelx, MatType.CV_64F, 1, 0);
                Cv2.Sobel(gray, imgSobely, MatType.CV_64F, 0, 1);
                Mat absGx = new Mat();
                Mat absGy = new Mat();
                Cv2.Absdiff(imgSobelx, Scalar.All(0), absGx);
                Cv2.Absdiff(imgSobely, Scalar.All(0), absGy);
                //Mat mag = new Mat();
                //Cv2.Magnitude(imgSobelx, imgSobely, mag);
                //MatExpr mag2 = imgSobelx.Mul(imgSobelx) + imgSobely.Mul(imgSobely);
                //double sum = Cv2.Sum(mag2).Val0;
                Mat mag = absGx + absGy;
                double sum = Cv2.Sum(mag).Val0;
                sharpness[0] = sum / (gray.Rows * gray.Cols);
                sharpness[1] = sharpness[0];
                sharpness[2] = sharpness[0];
                sharpness[3] = sharpness[0];
                sharpness[4] = sharpness[0];

                if (saveImg)
                {
                    Cv2.ImWrite("warped.bmp", warped);
                }
                srcImg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(warped);
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }



        public RotatedRect NormalizeRotatedRect(RotatedRect rr)
        {
            float width = rr.Size.Width;
            float height = rr.Size.Height;
            float angle = rr.Angle;

            if (width < height)
            {
                float tmp = width;
                width = height;
                height = tmp;

                angle += 90;
            }

            if (angle >= 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;

            return new RotatedRect(rr.Center, new OpenCvSharp.Size2f(width, height), angle);
        }
    }
}
