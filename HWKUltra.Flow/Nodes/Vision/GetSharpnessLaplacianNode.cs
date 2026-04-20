using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Vision.Algorithms.Focus;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Compute Laplacian-based sharpness score on a bitmap held in FlowContext.
    /// Expected input: a Bitmap under the context variable named by <c>BitmapVar</c>.
    /// </summary>
    public class GetSharpnessLaplacianNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Get Sharpness (Laplacian)";
        public override string NodeType => "GetSharpnessLaplacian";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitmapVar", DisplayName = "Bitmap Variable", Type = "string", Required = true, Description = "Context variable holding the Bitmap to score" }
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
                var varName = context.GetNodeInput<string>(Id, "BitmapVar") ?? "";
                if (string.IsNullOrEmpty(varName)) return FlowResult.Fail("BitmapVar is required");
                var bmp = context.GetVariable<Bitmap>(varName);
                if (bmp is null) return FlowResult.Fail($"Variable '{varName}' not found or not a Bitmap");
                var score = SharpnessLaplacian.Get(bmp);
                context.SetNodeOutput(Id, "Score", score);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"Laplacian sharpness failed: {ex.Message}"); }
        }
    }
}
