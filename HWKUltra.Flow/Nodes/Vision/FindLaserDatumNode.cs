using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Vision.Algorithms.Detection;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Locate laser datum center in a grayscale image buffer.
    /// Expects three context variables: <c>ImageDataVar</c> (byte[]), image height, image width.
    /// </summary>
    public class FindLaserDatumNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Find Laser Datum";
        public override string NodeType => "FindLaserDatum";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "ImageDataVar", DisplayName = "Image Data Var", Type = "string", Required = true },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = true },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = true }
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
                var varName = context.GetNodeInput<string>(Id, "ImageDataVar") ?? "";
                var h = context.GetNodeInput<int>(Id, "Height");
                var w = context.GetNodeInput<int>(Id, "Width");
                var data = context.GetVariable<byte[]>(varName);
                if (data is null) return FlowResult.Fail($"Variable '{varName}' not found or not a byte[]");
                var p = LaserDatumFinder.GetCenter(data, h, w, "");
                context.SetNodeOutput(Id, "X", p.X);
                context.SetNodeOutput(Id, "Y", p.Y);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"FindLaserDatum failed: {ex.Message}"); }
        }
    }
}
