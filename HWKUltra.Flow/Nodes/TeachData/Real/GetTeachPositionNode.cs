using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.TeachData.Real
{
    /// <summary>
    /// Reads a named teach position and outputs its axis values.
    /// No hardware dependency — only requires TeachDataService.
    /// </summary>
    public class GetTeachPositionNode : LogicNodeBase
    {
        private readonly TeachDataService? _teachData;

        public override string Name { get; set; } = "Get Teach Position";
        public override string NodeType => "GetTeachPosition";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Required = true, Description = "Teach position name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "X", DisplayName = "X", Type = "double", Description = "X axis value (0 if not present)" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "double", Description = "Y axis value (0 if not present)" },
            new FlowParameter { Name = "Z", DisplayName = "Z", Type = "double", Description = "Z axis value (0 if not present)" },
            new FlowParameter { Name = "Found", DisplayName = "Found", Type = "bool", Description = "Whether the position was found" },
            new FlowParameter { Name = "AxisCount", DisplayName = "Axis Count", Type = "int", Description = "Number of axes in the position" }
        };

        public GetTeachPositionNode(TeachDataService? teachData)
        {
            _teachData = teachData;
        }

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var posName = context.GetNodeInput<string>(Id, "PositionName") ?? "";

            if (_teachData == null)
            {
                context.SetNodeOutput(Id, "Found", false);
                context.SetNodeOutput(Id, "X", 0.0);
                context.SetNodeOutput(Id, "Y", 0.0);
                context.SetNodeOutput(Id, "Z", 0.0);
                context.SetNodeOutput(Id, "AxisCount", 0);
                return Task.FromResult(FlowResult.Fail("TeachDataService not available"));
            }

            var tp = _teachData.GetPosition(posName);
            if (tp == null)
            {
                Console.WriteLine($"[TeachData] Position '{posName}' not found");
                context.SetNodeOutput(Id, "Found", false);
                context.SetNodeOutput(Id, "X", 0.0);
                context.SetNodeOutput(Id, "Y", 0.0);
                context.SetNodeOutput(Id, "Z", 0.0);
                context.SetNodeOutput(Id, "AxisCount", 0);
                return Task.FromResult(FlowResult.Ok());
            }

            var pos = tp.ToAxisPosition();
            context.SetNodeOutput(Id, "Found", true);
            context.SetNodeOutput(Id, "X", pos.GetValueOrDefault("X"));
            context.SetNodeOutput(Id, "Y", pos.GetValueOrDefault("Y"));
            context.SetNodeOutput(Id, "Z", pos.GetValueOrDefault("Z"));
            context.SetNodeOutput(Id, "AxisCount", pos.Count);

            Console.WriteLine($"[TeachData] Got position '{posName}': {pos}");
            return Task.FromResult(FlowResult.Ok());
        }
    }
}
