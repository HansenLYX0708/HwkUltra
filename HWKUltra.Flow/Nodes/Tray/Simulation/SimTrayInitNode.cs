using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Simulation
{
    public class SimTrayInitNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Tray Init (Sim)";
        public override string NodeType => "TrayInit";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Rows", DisplayName = "Rows", Type = "int", Required = true },
            new FlowParameter { Name = "Cols", DisplayName = "Cols", Type = "int", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayInit: {name}");
            await Task.Delay(50, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
