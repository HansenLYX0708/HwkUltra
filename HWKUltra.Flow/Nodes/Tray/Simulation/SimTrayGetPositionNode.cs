using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Simulation
{
    public class SimTrayGetPositionNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Tray GetPosition (Sim)";
        public override string NodeType => "TrayGetPosition";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Row", DisplayName = "Row", Type = "int", Required = true },
            new FlowParameter { Name = "Col", DisplayName = "Col", Type = "int", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "X", DisplayName = "X", Type = "double" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "double" },
            new FlowParameter { Name = "Z", DisplayName = "Z", Type = "double" },
            new FlowParameter { Name = "AxisPositionJson", DisplayName = "AxisPosition JSON", Type = "string", Description = "Position as AxisPosition JSON for motion nodes" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayGetPosition: {name}");
            await Task.Delay(10, context.CancellationToken);
            context.SetNodeOutput(Id, "X", 100.0);
            context.SetNodeOutput(Id, "Y", 200.0);
            context.SetNodeOutput(Id, "Z", 0.0);
            context.SetNodeOutput(Id, "AxisPositionJson", Pos.XYZ(100.0, 200.0, 0.0).ToJson());
            return FlowResult.Ok();
        }
    }
}
