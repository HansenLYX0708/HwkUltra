using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Simulation
{
    public class SimTrayGetSlotStateNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Tray GetSlotState (Sim)";
        public override string NodeType => "TrayGetSlotState";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Row", DisplayName = "Row", Type = "int", Required = true },
            new FlowParameter { Name = "Col", DisplayName = "Col", Type = "int", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "State", DisplayName = "State", Type = "int" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayGetSlotState: {name}");
            await Task.Delay(10, context.CancellationToken);
            context.SetNodeOutput(Id, "State", 0);
            return FlowResult.Ok();
        }
    }
}
