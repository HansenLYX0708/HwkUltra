using HWKUltra.Measurement.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Real
{
    /// <summary>
    /// Measurement set sampling node - set the sampling cycle in microseconds.
    /// </summary>
    public class MeasurementSetSamplingNode : DeviceNodeBase<MeasurementRouter>
    {
        public override string Name { get; set; } = "Measurement SetSampling";
        public override string NodeType => "MeasurementSetSampling";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Measurement device instance name" },
            new FlowParameter { Name = "CycleUs", DisplayName = "Cycle (us)", Type = "int", Required = true, Description = "Sampling cycle in microseconds (100, 200, 500, 1000)" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public MeasurementSetSamplingNode(MeasurementRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                int cycleUs = context.GetNodeInput<int>(Id, "CycleUs");
                Service!.SetSamplingCycle(name, cycleUs);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Measurement set sampling failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            var cycle = context.GetNodeInput<int>(Id, "CycleUs");
            Console.WriteLine($"[SIMULATION] MeasurementSetSampling: {name} cycle={cycle}us");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
