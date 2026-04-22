using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using HWKUltra.Camera.Abstractions;
using HWKUltra.Camera.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Real
{
    /// <summary>
    /// Streaming camera capture node: subscribes to the router's ImageGrabbed event and
    /// pushes each frame (as a Bitmap) into a named <see cref="ImagePool"/>. Blocks
    /// until MaxFrames are captured, an external stop signal is received, a timeout
    /// elapses, or cancellation is requested — then unsubscribes, stops grabbing and
    /// calls <see cref="ImagePool.CompleteAdding"/> so the consumer can drain.
    /// </summary>
    public class CameraStartCaptureNode : DeviceNodeBase<CameraRouter>
    {
        public override string Name { get; set; } = "Camera Start Capture";
        public override string NodeType => "CameraStartCapture";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName",      DisplayName = "Camera Name",     Type = "string", Required = true,  Description = "Logical camera name" },
            new FlowParameter { Name = "PoolName",        DisplayName = "Pool Name",       Type = "string", Required = true,  DefaultValue = "ImagePool", Description = "Target ImagePool to push frames into" },
            new FlowParameter { Name = "MaxFrames",       DisplayName = "Max Frames",      Type = "int",    Required = false, DefaultValue = 0, Description = "Stop + CompleteAdding after N frames (0 = unlimited, rely on StopSignal / timeout)" },
            new FlowParameter { Name = "StopSignal",      DisplayName = "Stop Signal",     Type = "string", Required = false, Description = "SharedContext signal name that stops capture when set" },
            new FlowParameter { Name = "TimeoutMs",       DisplayName = "Timeout (ms)",    Type = "int",    Required = false, DefaultValue = 0, Description = "Overall timeout (0 = no timeout)" },
            new FlowParameter { Name = "AddTimeoutMs",    DisplayName = "Add Timeout (ms)",Type = "int",    Required = false, DefaultValue = 100, Description = "Per-frame back-pressure timeout; dropped on exceed" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "TotalEnqueued", DisplayName = "Total Enqueued", Type = "int" },
            new FlowParameter { Name = "TotalDropped",  DisplayName = "Total Dropped",  Type = "int" },
            new FlowParameter { Name = "StoppedBy",     DisplayName = "Stopped By",     Type = "string", Description = "MaxFrames | StopSignal | Timeout | Cancelled" }
        };

        protected override int SimulatedDelayMs => 0;

        public CameraStartCaptureNode(CameraRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "";
            var poolName   = context.GetNodeInput<string>(Id, "PoolName") ?? "ImagePool";
            var maxFrames  = context.GetNodeInput<int>(Id, "MaxFrames");
            var stopSignal = context.GetNodeInput<string>(Id, "StopSignal");
            var timeoutMs  = context.GetNodeInput<int>(Id, "TimeoutMs");
            var addTimeoutMs = context.GetNodeInput<int>(Id, "AddTimeoutMs");
            if (addTimeoutMs <= 0) addTimeoutMs = 100;

            if (string.IsNullOrEmpty(cameraName))
                return FlowResult.Fail("CameraName is required");

            if (context.SharedContext == null)
                return FlowResult.Fail("CameraStartCapture requires SharedContext");

            var pool = context.SharedContext.GetPool(poolName);
            if (pool == null)
                return FlowResult.Fail($"ImagePool '{poolName}' not found. Use ImagePoolCreate first.");

            int framesCaptured = 0;
            string stoppedBy = "";
            var capturedEvt = new ManualResetEventSlim(false);

            EventHandler<CameraImageEventArgs> handler = (s, e) =>
            {
                if (!string.Equals(e.CameraName, cameraName, StringComparison.Ordinal)) return;
                try
                {
                    var bmp = BytesToBitmap(e.ImageData, e.Width, e.Height, e.IsColor);
                    if (pool.TryAdd(bmp, addTimeoutMs, source: cameraName, ct: context.CancellationToken))
                    {
                        int count = Interlocked.Increment(ref framesCaptured);
                        if (maxFrames > 0 && count >= maxFrames)
                            capturedEvt.Set();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CameraStartCapture] frame conversion failed: {ex.Message}");
                }
            };

            Service!.ImageGrabbed += handler;
            try
            {
                if (!Service.StartGrabbing(cameraName))
                    return FlowResult.Fail($"StartGrabbing failed for {cameraName}");

                Console.WriteLine($"[CameraStartCapture] Started: camera={cameraName}, pool={poolName}, maxFrames={maxFrames}");

                // Wait until one of the stop conditions triggers.
                var stopTasks = new List<Task> { Task.Run(() => capturedEvt.Wait(context.CancellationToken), context.CancellationToken) };
                if (!string.IsNullOrEmpty(stopSignal))
                    stopTasks.Add(context.SharedContext.WaitForSignalAsync(stopSignal, -1, context.CancellationToken));
                if (timeoutMs > 0)
                    stopTasks.Add(Task.Delay(timeoutMs, context.CancellationToken));

                var which = await Task.WhenAny(stopTasks);
                if (context.CancellationToken.IsCancellationRequested)
                    stoppedBy = "Cancelled";
                else if (capturedEvt.IsSet)
                    stoppedBy = "MaxFrames";
                else if (!string.IsNullOrEmpty(stopSignal) && context.SharedContext.IsSignalSet(stopSignal))
                    stoppedBy = "StopSignal";
                else
                    stoppedBy = "Timeout";

                Console.WriteLine($"[CameraStartCapture] Stopped by {stoppedBy}: frames={framesCaptured}");
            }
            finally
            {
                try { Service.StopGrabbing(cameraName); } catch { /* best effort */ }
                Service.ImageGrabbed -= handler;
                pool.CompleteAdding();
            }

            context.SetNodeOutput(Id, "TotalEnqueued", (int)pool.TotalEnqueued);
            context.SetNodeOutput(Id, "TotalDropped", (int)pool.TotalDropped);
            context.SetNodeOutput(Id, "StoppedBy", stoppedBy);
            return FlowResult.Ok();
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "SimCam";
            var poolName   = context.GetNodeInput<string>(Id, "PoolName") ?? "ImagePool";
            var maxFrames  = context.GetNodeInput<int>(Id, "MaxFrames");
            if (maxFrames <= 0) maxFrames = 20;
            var interval   = context.GetNodeInput<int>(Id, "AddTimeoutMs");
            int intervalMs = interval > 0 ? interval : 50;

            if (context.SharedContext == null)
                return FlowResult.Fail("CameraStartCapture(Sim) requires SharedContext");
            var pool = context.SharedContext.GetPool(poolName);
            if (pool == null)
                return FlowResult.Fail($"ImagePool '{poolName}' not found");

            Console.WriteLine($"[SIMULATION] CameraStartCapture: camera={cameraName} pool={poolName} maxFrames={maxFrames}");

            int produced = 0;
            try
            {
                for (int i = 0; i < maxFrames; i++)
                {
                    if (context.CancellationToken.IsCancellationRequested) break;
                    var bmp = MakeSyntheticFrame(640, 480, i);
                    if (pool.TryAdd(bmp, 500, source: cameraName, ct: context.CancellationToken))
                        produced++;
                    await Task.Delay(intervalMs, context.CancellationToken);
                }
            }
            finally
            {
                pool.CompleteAdding();
            }

            context.SetNodeOutput(Id, "TotalEnqueued", produced);
            context.SetNodeOutput(Id, "TotalDropped", (int)pool.TotalDropped);
            context.SetNodeOutput(Id, "StoppedBy", context.CancellationToken.IsCancellationRequested ? "Cancelled" : "MaxFrames");
            return FlowResult.Ok();
        }

        private static Bitmap BytesToBitmap(byte[] data, int width, int height, bool isColor)
        {
            var fmt = isColor ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed;
            var bmp = new Bitmap(width, height, fmt);

            if (!isColor)
            {
                // 8bpp indexed requires a grayscale palette.
                var palette = bmp.Palette;
                for (int i = 0; i < 256; i++) palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                bmp.Palette = palette;
            }

            var rect = new Rectangle(0, 0, width, height);
            var bd = bmp.LockBits(rect, ImageLockMode.WriteOnly, fmt);
            try
            {
                int bytesPerPixel = isColor ? 3 : 1;
                int stride = bd.Stride;
                IntPtr scan0 = bd.Scan0;
                int rowBytes = width * bytesPerPixel;
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(data, y * rowBytes, scan0 + y * stride, Math.Min(rowBytes, Math.Max(0, data.Length - y * rowBytes)));
                }
            }
            finally { bmp.UnlockBits(bd); }
            return bmp;
        }

        private static Bitmap MakeSyntheticFrame(int w, int h, int frameIndex)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                // Use frameIndex to vary shade so sharpness/analysis can see differences.
                var shade = (byte)((frameIndex * 17) % 200 + 40);
                g.Clear(System.Drawing.Color.FromArgb(shade, shade, shade));
                using var font = new Font(FontFamily.GenericMonospace, 40);
                using var brush = new SolidBrush(System.Drawing.Color.White);
                g.DrawString($"Frame {frameIndex}", font, brush, 20, 20);
                // A few random black dots so Laplacian/variance produces non-zero values.
                var rng = new Random(frameIndex);
                using var dotBrush = new SolidBrush(System.Drawing.Color.Black);
                for (int i = 0; i < 80; i++)
                    g.FillRectangle(dotBrush, rng.Next(w), rng.Next(h), 3, 3);
            }
            return bmp;
        }
    }
}
