using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Simulation
{
    /// <summary>
    /// Simulated measurement set sampling node.
    /// </summary>
    public class SimMeasurementSetSamplingNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Measurement SetSampling (Sim)";
        public override string NodeType => "MeasurementSetSampling";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "CycleUs", DisplayName = "Cycle (us)", Type = "int", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            var cycle = context.GetNodeInput<int>(Id, "CycleUs");
            Console.WriteLine($"[SIMULATION] MeasurementSetSampling: {name} cycle={cycle}us");
            await Task.Delay(50, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
