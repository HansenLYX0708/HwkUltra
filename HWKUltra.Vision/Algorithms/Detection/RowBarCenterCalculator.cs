using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using HWKUltra.Core;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Calculates the geometric center of a row-bar in an image.
    /// Migrated verbatim from legacy WD.AVI.Vision.GetRowBarCenter.
    /// TODO: this file is large (~1200 lines) and contains many product-specific
    /// magic constants; consider splitting / parameterizing in a follow-up task.
    /// </summary>
    public class RowBarCenterCalculator
    {
        public Point3D CalcWindowTipEdge(byte[] data, int height, int width, int mode, bool isColor = false, bool saveImg = false, bool saveRaw = false, string rawpath = "", string rawName = "")
        {
            float scale = 0.5f;
            try
            {
                Mat source;
                if (isColor)
                {
                    source = new Mat(height, width, MatType.CV_8UC3, data);
                    if (saveRaw)
                    {
                        source.SaveImage(rawpath);
                    }
                    Cv2.Resize(source, source, OpenCvSharp.Size.Zero, scale, scale);

                    //Cv2.ImWrite("gray_mask.jpg", grayMask);
                    Cv2.CvtColor(source, source, ColorConversionCodes.BGR2GRAY);
                    
                }
                else
                {
                    source = new Mat(height, width, MatType.CV_8UC1, data);
                    if (saveRaw)
                    {
                        source.SaveImage(rawpath);
                    }
                }
                Mat pro = new Mat();
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchyIndices;

                Cv2.Blur(source, pro, new OpenCvSharp.Size(25, 25));
                Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Triangle);

                if (saveImg)
                {
                    //Cv2.ImWrite("Triangle.bmp", pro);
                }

                int size = 9;
                Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size), new OpenCvSharp.Point(-1, -1));
                Cv2.Dilate(pro, pro, se);

                if (saveImg)
                {
                    Cv2.ImWrite(rawName+ "_Dilate.bmp", pro);
                }

                Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

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
                    Mat blackImage = new Mat(new OpenCvSharp.Size(source.Width, source.Height), MatType.CV_8UC3, Scalar.Black);

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
                    Cv2.ImWrite(rawName + "_ErodeWithContours.bmp", blackImage);
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


                    if (alignRect.Size.Height < 300 * scale || alignRect.Size.Height > 3500 * scale)
                    {
                        return new Point3D(0, 0, 0);
                    }
                    else
                    {
                        Point2f centerTmp = new Point2f(0, 0);
                        if (mode == 1)
                        {
                            centerTmp = (alignRect.Points()[0] + alignRect.Points()[1]);
                        }
                        else if (mode == 2)
                        {
                            centerTmp = (alignRect.Points()[2] + alignRect.Points()[3]);
                        }

                        return new Point3D(centerTmp.X / 2 / scale, centerTmp.Y / 2 / scale, 0);
                    }
                }
                else
                {
                    return new Point3D(0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public Point3D CalcWindowTipEdgeColor1(byte[] data, int height, int width, int mode, bool isColor = false, bool saveImg = false, bool saveRaw = false, string rawpath = "")
        {
            try
            {
                Mat source;
                if (isColor)
                {
                    source = new Mat(height, width, MatType.CV_8UC3, data);
                    if (saveRaw)
                    {
                        source.SaveImage(rawpath);
                    }
                }
                else
                {
                    source = new Mat(height, width, MatType.CV_8UC1, data);
                    if (saveRaw)
                    {
                        source.SaveImage(rawpath);
                    }
                    return new Point3D(0, 0, 0);
                }

                Mat grayMask = new Mat(source.Rows, source.Cols, MatType.CV_8UC1, Scalar.All(0));

                int tolerance = 15;
                int minGray = 0;
                int maxGray = 120;

                for (int y = 0; y < source.Rows; y++)
                {
                    for (int x = 0; x < source.Cols; x++)
                    {
                        Vec3b pixel = source.At<Vec3b>(y, x);
                        byte b = pixel.Item0;
                        byte g = pixel.Item1;
                        byte r = pixel.Item2;

                        bool isGray =
                            Math.Abs(r - g) < tolerance &&
                            Math.Abs(r - b) < tolerance &&
                            Math.Abs(g - b) < tolerance &&
                            r > minGray && r < maxGray;

                        if (isGray)
                            grayMask.Set(y, x, 255);
                    }
                }

                int size1 = 9;
                Mat se1 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size1, size1), new OpenCvSharp.Point(-1, -1));
                Cv2.Dilate(grayMask, grayMask, se1);
                if (saveImg)
                {
                    Cv2.ImWrite("gray_mask.jpg", grayMask);
                }

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchyIndices;


                Cv2.FindContours(grayMask, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));


                int index = -1;
                bool set1st = false;
                double contour_area = 0;
                if (contours != null && contours.Length > 0)
                {
                    
                    for (int i = 0; i < contours.Length; i++)
                    {
                        RotatedRect rotatedRectTmp = Cv2.MinAreaRect(contours[i]);
                        RotatedRect alignRect = new RotatedRect();
                        if (rotatedRectTmp.Angle > 45)
                        {
                            Size2f newSize = new Size2f(rotatedRectTmp.Size.Height, rotatedRectTmp.Size.Width);
                            float newAngle = rotatedRectTmp.Angle - 90;
                            if (newAngle > 180) newAngle -= 360;
                            if (newAngle <= 180) newAngle += 360;

                            alignRect = new RotatedRect(rotatedRectTmp.Center, newSize, newAngle);
                        }
                        else
                        {
                            alignRect = rotatedRectTmp;
                        }


                        if (alignRect.Size.Height < 3500)
                        {
                            if (!set1st)
                            {
                                index = i;
                                contour_area = Cv2.ContourArea(contours[i]);
                                set1st = true;
                            }
                            double area_tmp = Cv2.ContourArea(contours[i]);
                            if (area_tmp > contour_area)
                            {
                                contour_area = area_tmp;
                                index = i;
                            }
                        }
                    }
                }

                if (saveImg && contours != null)
                {
                    int k = 0;
                    Mat blackImage = new Mat(new OpenCvSharp.Size(source.Width, source.Height), MatType.CV_8UC3, Scalar.Black);

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
                            if (i == index)
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


                    if (alignRect.Size.Height < 100 || alignRect.Size.Height > 3500)
                    {
                        return new Point3D(0, 0, 0);
                    }
                    else
                    {
                        Point2f centerTmp = new Point2f(0, 0);
                        if (mode == 1)
                        {
                            centerTmp = (alignRect.Points()[0] + alignRect.Points()[1]);
                        }
                        else if (mode == 2)
                        {
                            centerTmp = (alignRect.Points()[2] + alignRect.Points()[3]);
                        }

                        return new Point3D(centerTmp.X / 2, centerTmp.Y / 2, 0);
                    }
                }
                else
                {
                    return new Point3D(0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// mode = 1: Left 
        /// mode = 2: right 
        /// mode2 = 1: top
        /// mode2 = 2: bottom
        /// </summary>
        /// <param name="data"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="mode"></param>
        /// <param name="isColor"></param>
        /// <param name="saveImg"></param>
        /// <param name="saveRaw"></param>
        /// <param name="rawpath"></param>
        /// <returns></returns>
        public Point3D CalcWindowTipWithResistEdge(byte[] data, int height, int width, int mode, bool isColor = false, bool saveImg = false, bool saveRaw = false, string rawpath = "", string rawName = "", int mode2 = 1)
        {
            try
            {
                Mat source;
                Mat sourceColor = new Mat();
                if (isColor)
                {
                    sourceColor = new Mat(height, width, MatType.CV_8UC3, data);
                    if (saveRaw)
                    {
                        sourceColor.SaveImage(rawpath);
                    }
                    source = sourceColor;
                }
                else
                {
                    source = new Mat(height, width, MatType.CV_8UC1, data);
                    if (saveRaw)
                    {
                        source.SaveImage(rawpath);
                    }
                }


                Mat pro = new Mat();

                // resize
                double scale = 0.25;
                Mat resize = new Mat();
                Cv2.Resize(source, resize, OpenCvSharp.Size.Zero, scale, scale);
                Mat grayMask = new Mat(resize.Rows, resize.Cols, MatType.CV_8UC1, Scalar.All(0));
                int tolerance = 15;
                int minGray = 0;//80;
                int maxGray = 160;//160;

                for (int y = 0; y < resize.Rows; y++)
                {
                    for (int x = 0; x < resize.Cols; x++)
                    {
                        Vec3b pixel = resize.At<Vec3b>(y, x);
                        byte b = pixel.Item0;
                        byte g = pixel.Item1;
                        byte r = pixel.Item2;
                        bool isGray =
                            Math.Abs(r - g) < tolerance &&
                            Math.Abs(r - b) < tolerance &&
                            Math.Abs(g - b) < tolerance &&
                            r > minGray && r < maxGray;
                        if (isGray)
                            grayMask.Set(y, x, 255); // 白色掩码
                    }
                }
                int size1 = 9;
                Mat se1 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size1, size1), new OpenCvSharp.Point(-1, -1));
                Cv2.Dilate(grayMask, pro, se1);

                if (saveImg)
                {
                    // pro.ImWrite(rawName + "_grayMask.jpg");
                }
                LineSegmentPoint[] lineSegments =  Cv2.HoughLinesP(pro, 1, Math.PI / 180, 50, 100, 10);
                List<LineSegmentPoint> goodLines = new List<LineSegmentPoint>();

                foreach (var line in lineSegments)
                {
                    double len = Math.Sqrt(Math.Pow(line.P2.X - line.P1.X, 2) + Math.Pow(line.P2.Y - line.P1.Y, 2));
                    if (len > 150  && len < 800)
                    {
                        goodLines.Add(line);
                    }
                }

                // filter angle
                FilterLinesByAngle(goodLines, out var horizontalLines, out var verticalLines);

                if (saveImg)
                {
                    if (isColor)
                    {
                        Mat srcColorresize = new Mat();
                        Cv2.Resize(sourceColor, srcColorresize, OpenCvSharp.Size.Zero, scale, scale);
                        DrawLinesAndSave(srcColorresize, horizontalLines, verticalLines, rawName + "_lines.jpg");
                    }
                    else
                    {
                        DrawLinesAndSave(resize, horizontalLines, verticalLines, rawName + "_lines.jpg");
                    }
                }

                if (horizontalLines.Count < 1 || verticalLines.Count < 1)
                {
                    return new Point3D(0, 0, 0);
                }

                double retx = 0;
                double rety = 0;

                // find point base on position
                if (mode == 1)
                {
                    double minxmid = 9999;
                    // vertical line min
                    foreach (var line in verticalLines)
                    {
                        double xmid = (line.P1.X + line.P2.X) * 0.5;
                        if (xmid < minxmid)
                        {
                            minxmid = xmid;
                        }
                    }
                    retx = minxmid / scale;
                }
                else if (mode == 2) 
                {
                    double maxxmid = 0;
                    // vertical line max
                    foreach (var line in verticalLines)
                    {
                        double xmid = (line.P1.X + line.P2.X) * 0.5;
                        if (xmid > maxxmid)
                        {
                            maxxmid = xmid;
                        }
                    }
                    retx = maxxmid / scale;
                }


                if (mode2 == 1) // top
                {
                    List<Cluster> cluster = ClusterByMidY(horizontalLines);
                    // double ymid = CalculateY(cluster, 778, 40);

                    double minymid = 9999;
                    // horizontal line min
                    foreach (var clu in cluster)
                    {
                        if (clu.AverageY < minymid)
                        {
                            minymid = clu.AverageY;
                        }
                    }

                    if (minymid > height * scale * 0.5)
                    {
                        // can't find, use certical
                        minymid = minymid - 778 / 2;
                    }
                    else
                    {
                        minymid = minymid + 778 / 2;
                    }
                    rety = minymid / scale;
                }
                else if (mode2 == 2) // bottom
                {
                    List<Cluster> cluster = ClusterByMidY(horizontalLines);
                    // double ymid = CalculateY(cluster, 778, 40);

                    double maxymid = 0;
                    // horizontal line min
                    foreach (var clu in cluster)
                    {
                        if (clu.AverageY > maxymid)
                        {
                            maxymid = clu.AverageY;
                        }
                    }

                    if (maxymid < height * scale * 0.5)
                    {
                        // can't find, use certical
                        maxymid = maxymid + 778 / 2;
                    }
                    else
                    {
                        maxymid = maxymid - 778 / 2;
                    }
                    rety = maxymid / scale;
                }


                return new Point3D(retx, rety, 0);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        public Point3D FindWindowTipWithResistEdgeExistBar(byte[] data, int height, int width, int mode, bool isColor = false, bool saveImg = false, bool saveRaw = false, string rawpath = "", string rawName = "")
        {
            Point3D ret = new Point3D(0, 0, 0);
            double scale = 1.0;
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
                    Cv2.Resize(sourceColor, sourceColor, OpenCvSharp.Size.Zero, scale, scale);
                    Cv2.CvtColor(sourceColor, source, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    throw new Exception("FindWindowTipWithResistEdgeExistBar not support gray input!");
                }
                Mat pro = new Mat();
                Cv2.Blur(source, pro, new OpenCvSharp.Size(5, 5));
                Cv2.Threshold(pro, pro, 200, 255, ThresholdTypes.Binary);
                //InputArray kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3), new OpenCvSharp.Point(-1, -1));
                //Cv2.MorphologyEx(pro, pro, MorphTypes.Close, kernel, new OpenCvSharp.Point(-1, -1), 1, BorderTypes.Constant, Scalar.Gold);
                if (saveImg)
                {
                    pro.ImWrite(rawName + "_binary.jpg");
                }
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchyIndices;
                Cv2.FindContours(pro, out contours, out hierarchyIndices, RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                List<Point2f> poleTipList = new List<Point2f>();
                if (contours != null && contours.Length > 0)
                {
                    for (int i = 0; i < contours.Length; i++)
                    {
                        RotatedRect roR = Cv2.MinAreaRect(contours[i]);
                        LongEdgeRect ler = RotatedRectConverter.ToLongEdgeRect(roR);

                        if (Math.Abs(ler.Angle) < 10)
                        {
                            if (ler.Width > 43 && ler.Width < 70 &&
                                ler.Height > 7 && ler.Height < 40)
                            {
                                poleTipList.Add(ler.Center);
                            }
                        }
                    }
                }
                double retx = 0;
                double rety = 0;

                if (poleTipList.Count > 0)
                {
                    if (poleTipList.Count > 1)
                    {
                        // get min Y
                        double minY = double.MaxValue;
                        foreach(var point in poleTipList)
                        {
                            if (point.Y < minY)
                            {
                                minY = point.Y;
                                retx = point.X;
                                rety = point.Y;
                            }
                        }
                    }
                    else
                    {
                        retx = poleTipList[0].X;
                        rety = poleTipList[0].Y;
                    }
                }

                ret.X = retx / scale;
                ret.Y = rety / scale;

                return ret;
            }
            catch (Exception ex) 
            {
                throw ex;
            }

        }



        private  void FilterLinesByAngle(
        List<LineSegmentPoint> allLines,
        out List<LineSegmentPoint> horizontalLines,
        out List<LineSegmentPoint> verticalLines,
        double angleThreshold = 1.5)
        {
            horizontalLines = new List<LineSegmentPoint>();
            verticalLines = new List<LineSegmentPoint>();

            foreach (var line in allLines)
            {
                double dx = line.P2.X - line.P1.X;
                double dy = line.P2.Y - line.P1.Y;
                double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;

                angle = Math.Abs(angle);
                if (angle > 180) angle -= 180;

                if (angle < angleThreshold || Math.Abs(angle - 180) < angleThreshold)
                {
                    horizontalLines.Add(line);
                }
                else if (Math.Abs(angle - 90) < angleThreshold)
                {
                    verticalLines.Add(line);
                }
            }
        }

        public void DrawLinesAndSave(Mat originalImage, IEnumerable<LineSegmentPoint> lines1, IEnumerable<LineSegmentPoint> lines2, string outputPath)
        {
            Mat imageWithLines = originalImage.Clone();

            foreach (var line in lines1)
            {
                Cv2.Line(imageWithLines,
                         line.P1,
                         line.P2,
                         new Scalar(0, 0, 255), 
                         2,                  
                         LineTypes.AntiAlias);  
            }
            foreach (var line in lines2)
            {
                Cv2.Line(imageWithLines,
                         line.P1,
                         line.P2,
                         new Scalar(0, 0, 255),
                         2,                  
                         LineTypes.AntiAlias); 
            }

            Cv2.ImWrite(outputPath, imageWithLines);
        }

        /// <summary>
        /// find Horizontal tray blob and return edge center position
        /// mode = 1 : left edge
        /// mode = 2 : right edge
        /// </summary>
        /// <param name="data"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="isColor"></param>
        /// <param name="saveImg"></param>
        /// <returns></returns>
        public Point3D CalcHorizontalEdge(byte[] data, int height, int width, int mode, bool isColor = false, bool saveImg = false, bool saveRaw = false, string rawpath = "")
        {
            Mat source;
            float scale = 0.5f;
            if (isColor)
            {
                Mat sourceTmp = new Mat();
                source = new Mat();
                sourceTmp = new Mat(height, width, MatType.CV_8UC3, data);
                if (saveRaw)
                {
                    sourceTmp.SaveImage(rawpath);
                }
                Cv2.CvtColor(sourceTmp, source, ColorConversionCodes.BGR2GRAY);
                Cv2.Resize(source, source, OpenCvSharp.Size.Zero, scale, scale);
            }
            else
            {
                source = new Mat(height, width, MatType.CV_8UC1, data);
                if (saveRaw)
                {
                    source.SaveImage(rawpath);
                }
                Cv2.Resize(source, source, OpenCvSharp.Size.Zero, scale, scale);
            }
            Mat pro = new Mat();
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndices;

            Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
            Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);

            if (saveImg)
            {
                Cv2.ImWrite("Otsu.bmp", pro);
            }

            int size = 7;
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
                Mat blackImage = new Mat(new OpenCvSharp.Size(source.Width, source.Height), MatType.CV_8UC3, Scalar.Black);

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


                if (alignRect.Size.Height < 1400 * scale || alignRect.Size.Height > 3500 * scale)
                {
                    return new Point3D(0, 0, 0);
                }
                else
                {
                    Point2f centerTmp = new Point2f(0, 0);
                    if (mode == 1)
                    {
                        centerTmp = (alignRect.Points()[0] + alignRect.Points()[1]);
                    }
                    else if (mode == 2)
                    {
                        centerTmp = (alignRect.Points()[2] + alignRect.Points()[3]);
                    }

                    return new Point3D(centerTmp.X / 2 / scale, centerTmp.Y / 2 / scale, 0);
                }
            }
            else
            {
                return new Point3D(0, 0, 0);
            }
        }


        /// <summary>
        /// find blob and return edge center position
        /// mode = 1 : left edge
        /// mode = 2 : right edge
        /// </summary>
        /// <param name="data"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="mode"></param>
        /// <param name="isColor"></param>
        /// <param name="saveImg"></param>
        /// <param name="saveRaw"></param>
        /// <param name="rawpath"></param>
        /// <returns></returns>
        public Point3D CalcVerticalEdge(byte[] data, int height, int width, int mode, bool isColor = false, bool saveImg = false, bool saveRaw = false, string rawpath="" )
        {
            try
            {
                Mat source;
                float scale = 0.5f;
                if (isColor)
                {
                    source = new Mat(height, width, MatType.CV_8UC3, data);
                    if (saveRaw)
                    {
                        source.SaveImage(rawpath);
                    }
                    Cv2.CvtColor(source, source, ColorConversionCodes.BGR2GRAY);
                    Cv2.Resize(source, source, OpenCvSharp.Size.Zero, scale, scale);
                }
                else
                {
                    source = new Mat(height, width, MatType.CV_8UC1, data);
                    if (saveRaw)
                    {
                        source.SaveImage(rawpath);
                    }
                    Cv2.Resize(source, source, OpenCvSharp.Size.Zero, scale, scale);
                    
                }
                Mat pro = new Mat();
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchyIndices;

                Cv2.GaussianBlur(source, pro, new OpenCvSharp.Size(3, 3), 1.5);
                //Cv2.Blur(pro, pro, new OpenCvSharp.Size(10, 10));
                //Cv2.CvtColor(pro, pro, ColorConversionCodes.BGR2GRAY);
                double rettmp = Cv2.Threshold(pro, pro, 0, 255, ThresholdTypes.Otsu);

                if (saveImg)
                {
                    Cv2.ImWrite("otsu.bmp", pro);
                }

                int size = (int)(100 * scale);
                int size2 = (int)(10 * scale);
                Mat se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(7, 7), new OpenCvSharp.Point(-1, -1));
                Cv2.MorphologyEx(pro, pro, MorphTypes.Open, se);
                se = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(size, size2), new OpenCvSharp.Point(-1, -1));
                Cv2.MorphologyEx(pro, pro, MorphTypes.Close, se);

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
                    Mat blackImage = new Mat(new OpenCvSharp.Size(source.Width, source.Height), MatType.CV_8UC3, Scalar.Black);

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


                    if (alignRect.Size.Height < 300 * scale || alignRect.Size.Height > 1000 * scale)
                    {
                        return new Point3D(0, 0, 0);
                    }
                    else
                    {
                        Point2f centerTmp = new Point2f(0, 0);
                        if (mode == 1)
                        {
                            centerTmp = (alignRect.Points()[0] + alignRect.Points()[1]);
                        }
                        else if (mode == 2)
                        {
                            centerTmp = (alignRect.Points()[2] + alignRect.Points()[3]);
                        }

                        // resize to 1/2, so need x 2
                        return new Point3D(centerTmp.X / 2 / scale, centerTmp.Y / 2 / scale, 0);
                    }
                }
                else
                {
                    return new Point3D(0, 0, 0);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }




        #region assistant fucntion
        public class Cluster
        {
            public List<LineSegmentPoint> Lines { get; } = new List<LineSegmentPoint>();
            public double AverageY { get; set; }
            public LineSegmentPoint AveragedLine { get; set; }
        }
        public List<Cluster> ClusterByMidY(IEnumerable<LineSegmentPoint> lines, double yTolerance = 5.0, int minLinesInCluster = 1, bool useMedian = false)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            // 1. 计算中点 Y 并排序
            var items = lines
                .Select(l => new { Line = l, MidY = (l.P1.Y + l.P2.Y) * 0.5 })
                .OrderBy(it => it.MidY)
                .ToList();

            var clusters = new List<Cluster>();
            if (items.Count == 0) return clusters;

            // 2. 贪心按相邻差分组
            var curClusterLines = new List<LineSegmentPoint> { items[0].Line };
            var curMidYs = new List<double> { items[0].MidY };

            for (int i = 1; i < items.Count; i++)
            {
                double gap = items[i].MidY - items[i - 1].MidY;
                if (gap <= yTolerance)
                {
                    curClusterLines.Add(items[i].Line);
                    curMidYs.Add(items[i].MidY);
                }
                else
                {
                    // 完成当前簇
                    if (curClusterLines.Count >= minLinesInCluster)
                    {
                        clusters.Add(MakeCluster(curClusterLines, curMidYs, useMedian));
                    }
                    // 新簇
                    curClusterLines = new List<LineSegmentPoint> { items[i].Line };
                    curMidYs = new List<double> { items[i].MidY };
                }
            }

            // 写入最后一簇
            if (curClusterLines.Count >= minLinesInCluster)
            {
                clusters.Add(MakeCluster(curClusterLines, curMidYs, useMedian));
            }

            return clusters;
        }

        private Cluster MakeCluster(List<LineSegmentPoint> lines, List<double> midYs, bool useMedian)
        {
            var cluster = new Cluster();
            cluster.Lines.AddRange(lines);

            if (useMedian)
            {
                midYs.Sort();
                int n = midYs.Count;
                cluster.AverageY = (n % 2 == 1) ? midYs[n / 2] : (midYs[n / 2 - 1] + midYs[n / 2]) / 2.0;
            }
            else
            {
                cluster.AverageY = midYs.Average();
            }

            cluster.AveragedLine = AverageLines(lines);
            return cluster;
        }

        private LineSegmentPoint AverageLines(IEnumerable<LineSegmentPoint> lines)
        {
            double sx1 = 0, sy1 = 0, sx2 = 0, sy2 = 0;
            int n = 0;
            foreach (var l in lines)
            {
                sx1 += l.P1.X; sy1 += l.P1.Y;
                sx2 += l.P2.X; sy2 += l.P2.Y;
                n++;
            }
            if (n == 0) return new LineSegmentPoint(new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(0, 0));
            return new LineSegmentPoint(new OpenCvSharp.Point(sx1 / n, sy1 / n), new OpenCvSharp.Point(sx2 / n, sy2 / n));
        }

        private double CalculateY(List<Cluster> clusters, double targetDiff, double tolerance)
        {

            var pairs = new List<(double y1, double y2, double avg)>();

            // 寻找相差接近 targetDiff 的一对
            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    double diff = Math.Abs(clusters[j].AverageY - clusters[i].AverageY);
                    if (Math.Abs(diff - targetDiff) <= tolerance)
                    {
                        double avg = (clusters[i].AverageY + clusters[j].AverageY) / 2.0;
                        pairs.Add((clusters[i].AverageY, clusters[j].AverageY, avg));
                    }
                }
            }
            if (pairs.Count != 0)
                return pairs[0].avg;
            else
                return 0;

        }

        #endregion


    }

    public struct LongEdgeRect
    {
        public Point2f Center;
        public float Width;   // 长边
        public float Height;  // 短边
        public float Angle;   // 长边相对于水平轴的角度，单位度，范围 ~ (-90, 90]

        public LongEdgeRect(Point2f center, float width, float height, float angle)
        {
            Center = center;
            Width = width;
            Height = height;
            Angle = angle;
        }

        public override string ToString()
        {
            return $"Center={Center}, Width={Width}, Height={Height}, Angle={Angle}°";
        }
    }

    public static class RotatedRectConverter
    {
        /// <summary>
        /// 将 OpenCV/ OpenCvSharp 的 RotatedRect 转换为以长边为基准、角度范围约为 (-90, 90] 的表示：
        /// - Width 始终为长边
        /// - Angle 为长边相对于水平轴的角度，正为逆时针
        /// - 保证描述的矩形与原始 RotatedRect 完全一致
        /// </summary>
        public static LongEdgeRect ToLongEdgeRect(RotatedRect rr)
        {
            float angle = rr.Angle;
            float w = rr.Size.Width;
            float h = rr.Size.Height;
            Point2f center = rr.Center;

            // 1) 先把角度标准化到 (-180, 180]
            while (angle <= -180f) angle += 360f;
            while (angle > 180f) angle -= 360f;

            // 2) 把角度先映射到 (-90,90] 范围内（如果 angle 超出），以便更直观
            //    例如 angle=135 -> -225+360? 但下面处理更稳健：
            if (angle > 90f)
                angle -= 180f;
            else if (angle <= -90f)
                angle += 180f;

            // 3) 确保宽为长边。如果当前 w < h，则交换并把角度加 90°（长边方向随之旋转）
            if (w < h)
            {
                float tmp = w;
                w = h;
                h = tmp;
                angle += 90f;

                // 重新把 angle 拉回 (-90,90]（以防 angle 超过界限）
                if (angle > 90f) angle -= 180f;
                else if (angle <= -90f) angle += 180f;
            }

            // 最终返回（Angle 的语义：长边相对于水平轴的角度，正为逆时针）
            return new LongEdgeRect(center, w, h, angle);
        }
    }

}
