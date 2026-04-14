using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Simulation
{
    /// <summary>
    /// Simulated measurement stop storage node.
    /// </summary>
    public class SimMeasurementStopStorageNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Measurement StopStorage (Sim)";
        public override string NodeType => "MeasurementStopStorage";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] MeasurementStopStorage: Stop on {name}");
            await Task.Delay(50, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
