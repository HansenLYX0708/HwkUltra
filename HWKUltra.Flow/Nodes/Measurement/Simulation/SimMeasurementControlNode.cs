using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Simulation
{
    /// <summary>
    /// Simulated measurement control node.
    /// </summary>
    public class SimMeasurementControlNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Measurement Control (Sim)";
        public override string NodeType => "MeasurementControl";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Enable", DisplayName = "Enable", Type = "bool", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            var enable = context.GetNodeInput<string>(Id, "Enable") == "true";
            Console.WriteLine($"[SIMULATION] MeasurementControl: {name} enable={enable}");
            await Task.Delay(50, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
