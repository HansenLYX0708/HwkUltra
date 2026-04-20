using System.Drawing;
using System.Drawing.Imaging;
using HWKUltra.Core;
using HWKUltra.Vision.Abstractions;
using HWKUltra.Vision.Algorithms.Focus;
using HWKUltra.Vision.Algorithms.Rendering;
using HWKUltra.Vision.Inference;
using HWKUltra.Flow.Nodes.Vision;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Tests for HWKUltra.Vision algorithms (focus metrics, simulated inference,
    /// VisionDetection → DefectDetail mapper). Uses synthetic images; no external
    /// sample files needed.
    /// </summary>
    public static class VisionAlgorithmTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("  Vision Algorithm Tests");
            Console.WriteLine("=======================================");

            int pass = 0, fail = 0;
            var tests = new (string, Action)[]
            {
                ("Test 1: SharpnessLaplacian on uniform bitmap", Test_SharpnessLaplacian_Uniform),
                ("Test 2: SharpnessVar on noisy bitmap > uniform", Test_SharpnessVar_NoiseGreater),
                ("Test 3: Tenengrad on uniform bitmap", Test_Tenengrad_Uniform),
                ("Test 4: SimulatedInferenceEngine returns sentinel", Test_SimulatedInferenceEngine),
                ("Test 5: VisionDefectMapper preserves fields", Test_VisionDefectMapper_FieldsPreserved)
            };

            foreach (var (name, test) in tests)
            {
                try { test(); pass++; Console.WriteLine($"  [PASS] {name}"); }
                catch (Exception ex) { fail++; Console.WriteLine($"  [FAIL] {name}: {ex.Message}"); }
            }
            Console.WriteLine($"\nVision: {pass} passed, {fail} failed");
            if (fail > 0) throw new Exception($"{fail} vision tests failed");
        }

        private static Bitmap CreateUniformBitmap(int w, int h, byte gray)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            var pal = bmp.Palette;
            for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = pal;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] pixels = new byte[data.Stride * h];
            Array.Fill(pixels, gray);
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        private static Bitmap CreateNoisyBitmap(int w, int h, int seed)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            var pal = bmp.Palette;
            for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = pal;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] pixels = new byte[data.Stride * h];
            var rng = new Random(seed);
            rng.NextBytes(pixels);
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        public static void Test_SharpnessLaplacian_Uniform()
        {
            using var bmp = CreateUniformBitmap(64, 64, 128);
            double s = SharpnessLaplacian.Get(bmp);
            if (s < 0) throw new Exception($"expected non-negative, got {s}");
        }

        public static void Test_SharpnessVar_NoiseGreater()
        {
            using var uniform = CreateUniformBitmap(64, 64, 128);
            using var noisy = CreateNoisyBitmap(64, 64, 42);
            double su = SharpnessVar.Get(uniform);
            double sn = SharpnessVar.Get(noisy);
            if (sn <= su) throw new Exception($"noisy ({sn}) should exceed uniform ({su})");
        }

        public static void Test_Tenengrad_Uniform()
        {
            using var bmp = CreateUniformBitmap(64, 64, 100);
            double t = TenengradGradient.Get(bmp);
            if (t < 0) throw new Exception($"expected non-negative, got {t}");
        }

        public static void Test_SimulatedInferenceEngine()
        {
            var engine = new SimulatedInferenceEngine();
            engine.LoadModel();
            if (!engine.IsAvailable) throw new Exception("simulated engine must be available");
            using var bmp = CreateUniformBitmap(32, 32, 64);
            int[] result = engine.Predict(bmp);
            if (result.Length != 256) throw new Exception($"expected 256 slots, got {result.Length}");
            if (result[0] != -1) throw new Exception($"expected sentinel -1 at [0], got {result[0]}");
        }

        public static void Test_VisionDefectMapper_FieldsPreserved()
        {
            var v = new VisionDetection
            {
                Category = "A2",
                Confidence = 0.87f,
                Region = new BoundingBox(10, 20, 100, 200)
            };
            var d = VisionDefectMapper.ToDefectDetail(v, row: 3, col: 7, imgRows: 640, imgCols: 480);
            if (d.Row != 3) throw new Exception("row mismatch");
            if (d.Col != 7) throw new Exception("col mismatch");
            if (d.ImgRows != 640 || d.ImgCols != 480) throw new Exception("img dimensions mismatch");
            if (d.DefectCode != "A2") throw new Exception("defect code mismatch");
            if (Math.Abs(d.Confidence - 0.87f) > 0.001f) throw new Exception("confidence mismatch");
            if (d.Region is null || d.Region.X1 != 10 || d.Region.Y2 != 200) throw new Exception("region mismatch");
        }
    }
}
