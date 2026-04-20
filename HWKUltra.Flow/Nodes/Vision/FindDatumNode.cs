using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Vision.Algorithms.Detection;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Locate the crosshair-plus-circle datum in a bitmap.
    /// </summary>
    public class FindDatumNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Find Datum";
        public override string NodeType => "FindDatum";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitmapVar", DisplayName = "Bitmap Variable", Type = "string", Required = true },
            new FlowParameter { Name = "TemplatePath", DisplayName = "Template Path", Type = "string", Required = false, Description = "Optional override for DatumTemplate.bmp path" }
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
                var varName = context.GetNodeInput<string>(Id, "BitmapVar") ?? "";
                if (string.IsNullOrEmpty(varName)) return FlowResult.Fail("BitmapVar is required");
                var bmp = context.GetVariable<Bitmap>(varName);
                if (bmp is null) return FlowResult.Fail($"Variable '{varName}' not found or not a Bitmap");
                var templatePath = context.GetNodeInput<string>(Id, "TemplatePath");
                if (!string.IsNullOrEmpty(templatePath)) DatumFinder.SetTemplatePath(templatePath);
                var p = DatumFinder.Find(bmp);
                context.SetNodeOutput(Id, "X", p.X);
                context.SetNodeOutput(Id, "Y", p.Y);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"FindDatum failed: {ex.Message}"); }
        }
    }
}
