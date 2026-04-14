using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Simulation
{
    /// <summary>
    /// Simulated measurement clear storage node.
    /// </summary>
    public class SimMeasurementClearStorageNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Measurement ClearStorage (Sim)";
        public override string NodeType => "MeasurementClearStorage";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] MeasurementClearStorage: Clear on {name}");
            await Task.Delay(50, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
