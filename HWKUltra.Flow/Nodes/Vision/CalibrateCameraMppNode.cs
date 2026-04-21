using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;
using HWKUltra.Vision.Algorithms.Calibration;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Compute mm-per-pixel from a circle-grid calibration image.
    /// Accepts image as absolute path or context variable (Bitmap/Mat/byte[]/float[]/path).
    /// </summary>
    public class CalibrateCameraMppNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Calibrate Camera MPP";
        public override string NodeType => "CalibrateCameraMpp";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Image", DisplayName = "Image", Type = "string", Required = true, Description = "Absolute path OR context variable" },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = false },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = false },
            new FlowParameter { Name = "Channels", DisplayName = "Channels", Type = "int", Required = false, DefaultValue = 1 }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Mpp", DisplayName = "MPP (mm/pixel)", Type = "double" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var image = context.GetNodeInput<string>(Id, "Image") ?? "";
                if (string.IsNullOrEmpty(image)) return FlowResult.Fail("Image is required");
                int w = context.GetNodeInput<int>(Id, "Width");
                int h = context.GetNodeInput<int>(Id, "Height");
                int ch = context.GetNodeInput<int>(Id, "Channels"); if (ch == 0) ch = 1;
                using var resolved = ImageInputResolver.ResolveBitmap(context, image, w, h, ch);
                var mpp = CameraMppCalibrator.GetMPP(resolved.Bitmap);
                context.SetNodeOutput(Id, "Mpp", mpp);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"MPP calibration failed: {ex.Message}"); }
        }
    }
}
