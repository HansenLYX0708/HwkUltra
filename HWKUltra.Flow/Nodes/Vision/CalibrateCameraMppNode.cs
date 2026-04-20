using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Vision.Algorithms.Calibration;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Compute mm-per-pixel from a circle-grid calibration image.
    /// </summary>
    public class CalibrateCameraMppNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Calibrate Camera MPP";
        public override string NodeType => "CalibrateCameraMpp";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitmapVar", DisplayName = "Bitmap Variable", Type = "string", Required = true }
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
                var varName = context.GetNodeInput<string>(Id, "BitmapVar") ?? "";
                if (string.IsNullOrEmpty(varName)) return FlowResult.Fail("BitmapVar is required");
                var bmp = context.GetVariable<Bitmap>(varName);
                if (bmp is null) return FlowResult.Fail($"Variable '{varName}' not found or not a Bitmap");
                var mpp = CameraMppCalibrator.GetMPP(bmp);
                context.SetNodeOutput(Id, "Mpp", mpp);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"MPP calibration failed: {ex.Message}"); }
        }
    }
}
