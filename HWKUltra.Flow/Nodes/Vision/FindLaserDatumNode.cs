using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;
using HWKUltra.Vision.Algorithms.Detection;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Locate laser datum center in a grayscale image.
    /// Accepts image as absolute path or context variable (Bitmap/Mat/byte[]/float[]/path).
    /// For raw byte[] or float[] input, Width and Height are required.
    /// </summary>
    public class FindLaserDatumNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Find Laser Datum";
        public override string NodeType => "FindLaserDatum";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Image", DisplayName = "Image", Type = "string", Required = true, Description = "Absolute path OR context variable" },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = false, Description = "Required when input is raw byte[]/float[]" },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = false, Description = "Required when input is raw byte[]/float[]" },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable", Type = "string", Required = false, Description = "Shared-context name; stores Point at name, plus {name}_X / {name}_Y" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "X", DisplayName = "X", Type = "double" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "double" }
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
                var (data, rw, rh) = ImageInputResolver.ResolveBytes(context, image, w, h, isColor: false);
                var p = LaserDatumFinder.GetCenter(data, rh, rw, "");
                context.SetNodeOutput(Id, "X", p.X);
                context.SetNodeOutput(Id, "Y", p.Y);
                VisionOutput.PublishCompound(context, Id, "OutputVariable", p, ("X", p.X), ("Y", p.Y));
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"FindLaserDatum failed: {ex.Message}"); }
        }
    }
}
