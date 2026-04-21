using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Load a single image file into the flow context as a <see cref="Bitmap"/>.
    /// Downstream Vision nodes can consume it by setting Image = &lt;OutputVariable&gt;.
    /// </summary>
    public class LoadImageNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Load Image";
        public override string NodeType => "LoadImage";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Path", DisplayName = "Image Path", Type = "string", Required = true, Description = "Absolute path to the image file" },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable", Type = "string", Required = false, DefaultValue = "LoadedBitmap", Description = "Context variable name to store the loaded Bitmap" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int" },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int" },
            new FlowParameter { Name = "Path", DisplayName = "Resolved Path", Type = "string" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var path = context.GetNodeInput<string>(Id, "Path") ?? "";
                var outVar = context.GetNodeInput<string>(Id, "OutputVariable");
                if (string.IsNullOrWhiteSpace(outVar)) outVar = "LoadedBitmap";

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return Task.FromResult(FlowResult.Fail($"LoadImage: file not found: {path}"));

                var bmp = new Bitmap(path);
                context.Variables[outVar!] = bmp;
                context.Variables[outVar + "_Path"] = path;

                context.SetNodeOutput(Id, "Width", bmp.Width);
                context.SetNodeOutput(Id, "Height", bmp.Height);
                context.SetNodeOutput(Id, "Path", path);

                Console.WriteLine($"[LoadImage] {path} -> {outVar} ({bmp.Width}x{bmp.Height})");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"LoadImage failed: {ex.Message}"));
            }
        }
    }
}
