using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;
using HWKUltra.Vision.Algorithms.Focus;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Compute Laplacian-based sharpness score. Accepts image as absolute file path
    /// or as a FlowContext variable holding Bitmap / Mat / byte[] / float[] / path string.
    /// </summary>
    public class GetSharpnessLaplacianNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Get Sharpness (Laplacian)";
        public override string NodeType => "GetSharpnessLaplacian";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Image", DisplayName = "Image", Type = "string", Required = true, Description = "Absolute file path OR context variable name holding Bitmap/Mat/byte[]/float[]/path" },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = false, Description = "Required only for raw byte[]/float[] input" },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = false, Description = "Required only for raw byte[]/float[] input" },
            new FlowParameter { Name = "Channels", DisplayName = "Channels", Type = "int", Required = false, DefaultValue = 1, Description = "1/3/4 — only for raw byte[] input" },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable", Type = "string", Required = false, Description = "Shared-context name for the Score (downstream-friendly)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Score", DisplayName = "Score", Type = "double", Description = "Higher is sharper" }
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
                var score = SharpnessLaplacian.Get(resolved.Bitmap);
                context.SetNodeOutput(Id, "Score", score);
                VisionOutput.Publish(context, Id, "OutputVariable", score);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"Laplacian sharpness failed: {ex.Message}"); }
        }
    }
}
