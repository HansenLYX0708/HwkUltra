using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;
using HWKUltra.Vision.Algorithms.Detection;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Locate the crosshair-plus-circle datum in a bitmap.
    /// Accepts image as absolute path or context variable (Bitmap/Mat/byte[]/float[]/path).
    /// </summary>
    public class FindDatumNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Find Datum";
        public override string NodeType => "FindDatum";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Image", DisplayName = "Image", Type = "string", Required = true, Description = "Absolute path OR context variable" },
            new FlowParameter { Name = "TemplatePath", DisplayName = "Template Path", Type = "string", Required = false, Description = "Optional override for DatumTemplate.bmp path" },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = false },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = false },
            new FlowParameter { Name = "Channels", DisplayName = "Channels", Type = "int", Required = false, DefaultValue = 1 },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable", Type = "string", Required = false, Description = "Shared-context name; stores Point at name, plus {name}_X / {name}_Y" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "X", DisplayName = "X", Type = "int" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "int" }
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
                var templatePath = context.GetNodeInput<string>(Id, "TemplatePath");
                if (!string.IsNullOrEmpty(templatePath)) DatumFinder.SetTemplatePath(templatePath);
                var p = DatumFinder.Find(resolved.Bitmap);
                context.SetNodeOutput(Id, "X", p.X);
                context.SetNodeOutput(Id, "Y", p.Y);
                VisionOutput.PublishCompound(context, Id, "OutputVariable", p, ("X", p.X), ("Y", p.Y));
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"FindDatum failed: {ex.Message}"); }
        }
    }
}
