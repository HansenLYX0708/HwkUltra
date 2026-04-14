using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Simulation
{
    public class SimTraySetSlotStateNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Tray SetSlotState (Sim)";
        public override string NodeType => "TraySetSlotState";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Row", DisplayName = "Row", Type = "int", Required = true },
            new FlowParameter { Name = "Col", DisplayName = "Col", Type = "int", Required = true },
            new FlowParameter { Name = "State", DisplayName = "State", Type = "int", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TraySetSlotState: {name}");
            await Task.Delay(10, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
