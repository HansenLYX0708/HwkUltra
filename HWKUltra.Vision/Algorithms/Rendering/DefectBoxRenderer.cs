using System;
using System.Collections.Generic;
using System.IO;
using HWKUltra.Vision.Abstractions;
using OpenCvSharp;

namespace HWKUltra.Vision.Algorithms.Rendering
{
    /// <summary>
    /// Renders annotated defect boxes onto a source image and saves the result.
    /// Migrated from legacy WD.AVI.Vision.DrawBoxToImage.
    /// Legacy used <c>DetectsFormat</c> + <c>UtilsCfg.Category2rgb</c>; now uses
    /// <see cref="VisionDetection"/> + <see cref="IVisionConfig"/>.
    /// Source image size (used to scale bounding boxes from 640x640 detection space)
    /// is parameterizable via <paramref name="sourceSize"/>.
    /// </summary>
    public static class DefectBoxRenderer
    {
        public static void Draw(
            string path,
            string savePath,
            IReadOnlyList<VisionDetection> detections,
            IVisionConfig config,
            int sourceSize = 640)
        {
            if (detections == null) return;
            if (!File.Exists(path)) return;

            string[] pathsplit = path.Split('/');
            string fileLabel = pathsplit.Length >= 3 ? pathsplit[2] : System.IO.Path.GetFileName(path);

            Mat img = Cv2.ImRead(path, ImreadModes.Color);
            string row1;
            string row2 = "Defect code:";
            var defectCountDict = new Dictionary<string, int>();

            if (detections.Count == 0)
            {
                row1 = "File name:" + fileLabel + " Result:Good ";
                row2 += "No found.";
            }
            else
            {
                foreach (var detect in detections)
                {
                    var rect = new Rect(
                        Convert.ToInt32((double)detect.Region.X1 / sourceSize * img.Width),
                        Convert.ToInt32((double)detect.Region.Y1 / sourceSize * img.Height),
                        Convert.ToInt32(((double)detect.Region.X2 - detect.Region.X1) / sourceSize * img.Width),
                        Convert.ToInt32(((double)detect.Region.Y2 - detect.Region.Y1) / sourceSize * img.Height));

                    Scalar scalar = ResolveColor(detect.Category, config);
                    if (defectCountDict.ContainsKey(detect.Category))
                        defectCountDict[detect.Category]++;
                    else
                        defectCountDict.Add(detect.Category, 1);

                    Cv2.Rectangle(img, rect, scalar, 2);
                    Cv2.PutText(
                        img,
                        detect.Category,
                        new OpenCvSharp.Point(
                            Convert.ToInt32((double)detect.Region.X1 / sourceSize * img.Width),
                            Convert.ToInt32((double)detect.Region.Y1 / sourceSize * img.Height) - 5),
                        HersheyFonts.Italic,
                        0.8,
                        scalar,
                        2);
                }
                row1 = "File name:" + fileLabel + " Result:NG ";
                int index = 0;
                foreach (var item in defectCountDict)
                {
                    index++;
                    Scalar scalar = ResolveColor(item.Key, config);
                    Cv2.PutText(img, item.Key + ":" + item.Value.ToString(), new OpenCvSharp.Point(0, 80 + index * 40), HersheyFonts.Italic, 1, scalar, 2);
                }
            }
            Cv2.PutText(img, row1, new OpenCvSharp.Point(0, 40), HersheyFonts.Italic, 1, new Scalar(0, 255, 255), 2);
            Cv2.PutText(img, row2, new OpenCvSharp.Point(0, 80), HersheyFonts.Italic, 1, new Scalar(0, 255, 255), 2);
            Cv2.ImWrite(savePath, img);
        }

        private static Scalar ResolveColor(string category, IVisionConfig config)
        {
            if (config?.CategoryColors != null && config.CategoryColors.TryGetValue(category, out var color))
                return new Scalar(color.B, color.G, color.R);
            return new Scalar(0, 0, 255); // default red
        }
    }
}
