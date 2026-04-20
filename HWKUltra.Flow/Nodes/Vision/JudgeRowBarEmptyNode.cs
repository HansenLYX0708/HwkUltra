using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Vision.Algorithms.Judge;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Decide whether a row-bar image contains no slider.
    /// </summary>
    public class JudgeRowBarEmptyNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Judge Row-Bar Empty";
        public override string NodeType => "JudgeRowBarEmpty";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "ImageDataVar", DisplayName = "Image Data Var", Type = "string", Required = true },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = true },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = true },
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
                var varName = context.GetNodeInput<string>(Id, "ImageDataVar") ?? "";
                var h = context.GetNodeInput<int>(Id, "Height");
                var w = context.GetNodeInput<int>(Id, "Width");
                var isColor = context.GetNodeInput<bool>(Id, "IsColor");
                var data = context.GetVariable<byte[]>(varName);
                if (data is null) return FlowResult.Fail($"Variable '{varName}' not found or not a byte[]");
                var judge = new RowBarEmptyJudge();
                var isEmpty = judge.Judge(data, h, w, isColor);
                context.SetNodeOutput(Id, "IsEmpty", isEmpty);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"JudgeRowBarEmpty failed: {ex.Message}"); }
        }
    }
}
