using HWKUltra.Measurement.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Real
{
    /// <summary>
    /// Measurement clear storage node - clear stored data.
    /// </summary>
    public class MeasurementClearStorageNode : DeviceNodeBase<MeasurementRouter>
    {
        public override string Name { get; set; } = "Measurement ClearStorage";
        public override string NodeType => "MeasurementClearStorage";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Measurement device instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public MeasurementClearStorageNode(MeasurementRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                Service!.ClearStorage(name);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Measurement clear storage failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] MeasurementClearStorage: Clear on {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
