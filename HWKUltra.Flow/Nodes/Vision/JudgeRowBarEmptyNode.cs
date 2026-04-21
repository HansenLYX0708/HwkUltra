using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;
using HWKUltra.Vision.Algorithms.Judge;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Decide whether a row-bar image contains no slider.
    /// Accepts image as absolute path or context variable (Bitmap/Mat/byte[]/float[]/path).
    /// </summary>
    public class JudgeRowBarEmptyNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Judge Row-Bar Empty";
        public override string NodeType => "JudgeRowBarEmpty";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Image", DisplayName = "Image", Type = "string", Required = true, Description = "Absolute path OR context variable" },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = false, Description = "Required when input is raw byte[]/float[]" },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = false, Description = "Required when input is raw byte[]/float[]" },
            new FlowParameter { Name = "IsColor", DisplayName = "Is Color", Type = "bool", Required = false }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "IsEmpty", DisplayName = "Is Empty", Type = "bool" }
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
                var isColor = context.GetNodeInput<bool>(Id, "IsColor");
                var (data, rw, rh) = ImageInputResolver.ResolveBytes(context, image, w, h, isColor);
                var judge = new RowBarEmptyJudge();
                var isEmpty = judge.Judge(data, rh, rw, isColor);
                context.SetNodeOutput(Id, "IsEmpty", isEmpty);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"JudgeRowBarEmpty failed: {ex.Message}"); }
        }
    }
}
