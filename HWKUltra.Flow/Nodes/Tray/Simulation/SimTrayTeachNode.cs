using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Simulation
{
    public class SimTrayTeachNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Tray Teach (Sim)";
        public override string NodeType => "TrayTeach";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "LT_X", DisplayName = "LeftTop X", Type = "double" },
            new FlowParameter { Name = "LT_Y", DisplayName = "LeftTop Y", Type = "double" },
            new FlowParameter { Name = "LT_Z", DisplayName = "LeftTop Z", Type = "double" },
            new FlowParameter { Name = "RT_X", DisplayName = "RightTop X", Type = "double" },
            new FlowParameter { Name = "RT_Y", DisplayName = "RightTop Y", Type = "double" },
            new FlowParameter { Name = "RT_Z", DisplayName = "RightTop Z", Type = "double" },
            new FlowParameter { Name = "LB_X", DisplayName = "LeftBottom X", Type = "double" },
            new FlowParameter { Name = "LB_Y", DisplayName = "LeftBottom Y", Type = "double" },
            new FlowParameter { Name = "LB_Z", DisplayName = "LeftBottom Z", Type = "double" },
            new FlowParameter { Name = "RB_X", DisplayName = "RightBottom X", Type = "double" },
            new FlowParameter { Name = "RB_Y", DisplayName = "RightBottom Y", Type = "double" },
            new FlowParameter { Name = "RB_Z", DisplayName = "RightBottom Z", Type = "double" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayTeach: 4-corner teach for {name}");
            await Task.Delay(100, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
