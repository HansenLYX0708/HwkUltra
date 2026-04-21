using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using HWKUltra.Flow.Abstractions;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace HWKUltra.Flow.Utils
{
    /// <summary>
    /// Resolves a flexible vision-node image input into a concrete image form.
    /// The input value (a string) is interpreted as one of:
    ///   1. An absolute file path on disk → loaded from file
    ///   2. A context variable name whose value is one of:
    ///      - <see cref="Bitmap"/>
    ///      - <see cref="Mat"/>
    ///      - <see cref="string"/> (treated again as file path)
    ///      - <see cref="byte[]"/> (with shape hints)
    ///      - <see cref="float[]"/> (with shape hints; normalized [0..1] data)
    /// Shape hints (<paramref name="width"/> / <paramref name="height"/> / <paramref name="channels"/>)
    /// are only required when resolving raw buffers.
    /// </summary>
    public static class ImageInputResolver
    {
        /// <summary>A resolved bitmap + ownership info. When <see cref="Owned"/> is true the caller must dispose the bitmap.</summary>
        public readonly record struct ResolvedBitmap(Bitmap Bitmap, bool Owned) : IDisposable
        {
            public void Dispose() { if (Owned) Bitmap.Dispose(); }
        }

        /// <summary>A resolved Mat + ownership info. When <see cref="Owned"/> is true the caller must dispose the Mat.</summary>
        public readonly record struct ResolvedMat(Mat Mat, bool Owned) : IDisposable
        {
            public void Dispose() { if (Owned) Mat.Dispose(); }
        }

        /// <summary>
        /// Resolve the input to a Bitmap suitable for algorithms that take <see cref="Bitmap"/>.
        /// </summary>
        public static ResolvedBitmap ResolveBitmap(FlowContext ctx, string input, int width = 0, int height = 0, int channels = 1)
        {
            var (obj, hintedPath) = LookupSource(ctx, input);

            if (hintedPath != null)
                return new ResolvedBitmap(new Bitmap(hintedPath), true);

            return obj switch
            {
                Bitmap b => new ResolvedBitmap(b, false),
                Mat m => new ResolvedBitmap(BitmapConverter.ToBitmap(m), true),
                string s when File.Exists(s) => new ResolvedBitmap(new Bitmap(s), true),
                byte[] bytes => new ResolvedBitmap(BytesToBitmap(bytes, width, height, channels), true),
                float[] floats => new ResolvedBitmap(FloatsToBitmap(floats, width, height), true),
                _ => throw new InvalidOperationException(
                    $"Image input '{input}' resolved to unsupported type '{obj?.GetType().FullName ?? "null"}'. " +
                    "Expected: absolute file path, Bitmap, Mat, byte[], float[], or string path.")
            };
        }

        /// <summary>
        /// Resolve the input to an OpenCvSharp <see cref="Mat"/>.
        /// </summary>
        public static ResolvedMat ResolveMat(FlowContext ctx, string input, int width = 0, int height = 0, int channels = 1)
        {
            var (obj, hintedPath) = LookupSource(ctx, input);

            if (hintedPath != null)
                return new ResolvedMat(Cv2.ImRead(hintedPath, ImreadModes.Unchanged), true);

            return obj switch
            {
                Mat m => new ResolvedMat(m, false),
                Bitmap b => new ResolvedMat(BitmapConverter.ToMat(b), true),
                string s when File.Exists(s) => new ResolvedMat(Cv2.ImRead(s, ImreadModes.Unchanged), true),
                byte[] bytes => new ResolvedMat(BytesToMat(bytes, width, height, channels), true),
                float[] floats => new ResolvedMat(FloatsToMat(floats, width, height), true),
                _ => throw new InvalidOperationException(
                    $"Image input '{input}' resolved to unsupported type '{obj?.GetType().FullName ?? "null"}'.")
            };
        }

        /// <summary>
        /// Resolve the input to a grayscale byte[] buffer with its width/height.
        /// Used by algorithms (e.g. FindLaserDatum, JudgeRowBarEmpty) that want raw image data.
        /// If <paramref name="isColor"/> is true, returns a 3-channel interleaved BGR buffer.
        /// </summary>
        public static (byte[] Data, int Width, int Height) ResolveBytes(
            FlowContext ctx, string input, int width = 0, int height = 0, bool isColor = false)
        {
            var (obj, hintedPath) = LookupSource(ctx, input);

            Mat? mat = null;
            bool ownMat = true;
            try
            {
                if (hintedPath != null)
                {
                    mat = Cv2.ImRead(hintedPath, isColor ? ImreadModes.Color : ImreadModes.Grayscale);
                }
                else
                {
                    switch (obj)
                    {
                        case byte[] bytes when width > 0 && height > 0:
                            return (bytes, width, height);
                        case byte[] bytes:
                            throw new ArgumentException("Width/Height required when input is raw byte[]");
                        case Mat m:
                            mat = m; ownMat = false; break;
                        case Bitmap b:
                            mat = BitmapConverter.ToMat(b); break;
                        case string s when File.Exists(s):
                            mat = Cv2.ImRead(s, isColor ? ImreadModes.Color : ImreadModes.Grayscale); break;
                        case float[] floats:
                            using (var fm = FloatsToMat(floats, width, height))
                                mat = fm.Clone();
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Image input '{input}' resolved to unsupported type '{obj?.GetType().FullName ?? "null"}'.");
                    }
                }

                // Ensure desired channel layout
                if (!isColor && mat!.Channels() != 1)
                {
                    var gray = new Mat();
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                    if (ownMat) mat.Dispose();
                    mat = gray; ownMat = true;
                }
                else if (isColor && mat!.Channels() == 1)
                {
                    var bgr = new Mat();
                    Cv2.CvtColor(mat, bgr, ColorConversionCodes.GRAY2BGR);
                    if (ownMat) mat.Dispose();
                    mat = bgr; ownMat = true;
                }

                int w = mat!.Cols, h = mat.Rows;
                int len = w * h * mat.Channels();
                var buf = new byte[len];
                System.Runtime.InteropServices.Marshal.Copy(mat.Data, buf, 0, len);
                return (buf, w, h);
            }
            finally
            {
                if (ownMat) mat?.Dispose();
            }
        }

        // ---------- internal helpers ----------

        private static (object? Value, string? DirectPath) LookupSource(FlowContext ctx, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Image input is empty");

            // 1. Absolute file path
            if (Path.IsPathRooted(input) && File.Exists(input))
                return (null, input);

            // 2. Context variable
            if (ctx.Variables.TryGetValue(input, out var v) && v != null)
                return (v, null);

            throw new InvalidOperationException(
                $"Image input '{input}' is neither an existing absolute file path nor a defined context variable.");
        }

        private static Bitmap BytesToBitmap(byte[] bytes, int width, int height, int channels)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width/Height required for byte[] input");

            using var mat = BytesToMat(bytes, width, height, channels);
            return BitmapConverter.ToBitmap(mat);
        }

        private static Mat BytesToMat(byte[] bytes, int width, int height, int channels)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width/Height required for byte[] input");

            MatType type = channels switch
            {
                1 => MatType.CV_8UC1,
                3 => MatType.CV_8UC3,
                4 => MatType.CV_8UC4,
                _ => throw new ArgumentException($"Unsupported channel count: {channels}")
            };
            var mat = new Mat(height, width, type);
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, mat.Data, Math.Min(bytes.Length, width * height * channels));
            return mat;
        }

        private static Bitmap FloatsToBitmap(float[] floats, int width, int height)
        {
            using var mat = FloatsToMat(floats, width, height);
            return BitmapConverter.ToBitmap(mat);
        }

        private static Mat FloatsToMat(float[] floats, int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width/Height required for float[] input");

            // Normalize [min..max] → [0..255] and produce 8UC1.
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < floats.Length; i++)
            {
                if (floats[i] < min) min = floats[i];
                if (floats[i] > max) max = floats[i];
            }
            float range = max - min;
            if (range <= float.Epsilon) range = 1f;

            var buf = new byte[width * height];
            int n = Math.Min(buf.Length, floats.Length);
            for (int i = 0; i < n; i++)
            {
                float v = (floats[i] - min) / range * 255f;
                if (v < 0) v = 0; else if (v > 255) v = 255;
                buf[i] = (byte)v;
            }
            var mat = new Mat(height, width, MatType.CV_8UC1);
            System.Runtime.InteropServices.Marshal.Copy(buf, 0, mat.Data, buf.Length);
            return mat;
        }
    }
}
