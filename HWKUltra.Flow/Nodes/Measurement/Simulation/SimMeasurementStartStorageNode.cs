using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Simulation
{
    /// <summary>
    /// Simulated measurement start storage node.
    /// </summary>
    public class SimMeasurementStartStorageNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Measurement StartStorage (Sim)";
        public override string NodeType => "MeasurementStartStorage";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] MeasurementStartStorage: Start on {name}");
            await Task.Delay(50, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
