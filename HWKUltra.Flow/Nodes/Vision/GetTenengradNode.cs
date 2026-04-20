using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Vision.Algorithms.Focus;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Compute Tenengrad focus score on a bitmap held in FlowContext.
    /// </summary>
    public class GetTenengradNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Get Tenengrad";
        public override string NodeType => "GetTenengrad";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitmapVar", DisplayName = "Bitmap Variable", Type = "string", Required = true }
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
                var varName = context.GetNodeInput<string>(Id, "BitmapVar") ?? "";
                if (string.IsNullOrEmpty(varName)) return FlowResult.Fail("BitmapVar is required");
                var bmp = context.GetVariable<Bitmap>(varName);
                if (bmp is null) return FlowResult.Fail($"Variable '{varName}' not found or not a Bitmap");
                var score = TenengradGradient.Get(bmp);
                context.SetNodeOutput(Id, "Score", score);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"Tenengrad failed: {ex.Message}"); }
        }
    }
}
