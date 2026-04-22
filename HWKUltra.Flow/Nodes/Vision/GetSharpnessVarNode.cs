using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;
using HWKUltra.Vision.Algorithms.Focus;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Compute per-pixel std-dev sharpness score (<see cref="SharpnessVar"/>).
    /// Accepts image as absolute path or context variable (Bitmap / Mat / byte[] / float[] / path).
    /// </summary>
    public class GetSharpnessVarNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Get Sharpness (Variance)";
        public override string NodeType => "GetSharpnessVar";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Image", DisplayName = "Image", Type = "string", Required = true, Description = "Absolute path OR context variable holding Bitmap/Mat/byte[]/float[]/path" },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = false },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = false },
            new FlowParameter { Name = "Channels", DisplayName = "Channels", Type = "int", Required = false, DefaultValue = 1 },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable", Type = "string", Required = false, Description = "Shared-context name for the Score" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Score", DisplayName = "Score", Type = "double" }
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
                var score = SharpnessVar.Get(resolved.Bitmap);
                context.SetNodeOutput(Id, "Score", score);
                VisionOutput.Publish(context, Id, "OutputVariable", score);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"Variance sharpness failed: {ex.Message}"); }
        }
    }
}
