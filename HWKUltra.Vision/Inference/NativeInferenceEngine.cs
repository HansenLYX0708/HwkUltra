using System;
using System.Drawing;
using System.Runtime.InteropServices;
using HWKUltra.Vision.Abstractions;

namespace HWKUltra.Vision.Inference
{
    /// <summary>
    /// Native DL inference engine backed by main.dll (P/Invoke).
    /// Migrated verbatim (P/Invoke signatures preserved) from legacy WD.AVI.Vision.Inference.
    /// <see cref="IsAvailable"/> remains false if LoadModel fails, allowing callers
    /// to fall back to <see cref="SimulatedInferenceEngine"/> when main.dll is missing.
    /// </summary>
    public class NativeInferenceEngine : IInferenceEngine
    {
        [DllImport("main.dll", EntryPoint = "Loadmodel", CharSet = CharSet.Ansi)]
        private static extern void Loadmodel();

        [DllImport("main.dll", EntryPoint = "PredictImagePlus", CharSet = CharSet.Ansi)]
        private static extern void PredictImagePlus(
            int height, int width, int channels,
            byte[] srcImage, double threshold, bool runBenchmark, IntPtr result);

        public bool IsAvailable { get; private set; }

        public void LoadModel()
        {
            try
            {
                Loadmodel();
                IsAvailable = true;
            }
            catch
            {
                IsAvailable = false;
            }
        }

        public int[] Predict(Bitmap bmp)
        {
            int[] resultlist = new int[256];
            for (int i = 0; i < 6; i++) resultlist[i] = -1;

            double threshold = 0.5;
            bool run_benchmark = false;
            byte[] bgrSource = GetBGRValues(bmp, out _);
            IntPtr results = GetBytesPtrInt(resultlist);
            PredictImagePlus(bmp.Height, bmp.Width, 3, bgrSource, threshold, run_benchmark, results);
            return resultlist;
        }

        private static IntPtr GetBytesPtrInt(int[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

        private static byte[] GetBGRValues(Bitmap bmp, out int stride)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            stride = bmpData.Stride;
            var rowBytes = bmpData.Width * Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
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
    }
}
