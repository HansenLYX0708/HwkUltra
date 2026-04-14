using HWKUltra.Measurement.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Real
{
    /// <summary>
    /// Measurement control node - enable or disable measurement.
    /// </summary>
    public class MeasurementControlNode : DeviceNodeBase<MeasurementRouter>
    {
        public override string Name { get; set; } = "Measurement Control";
        public override string NodeType => "MeasurementControl";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Measurement device instance name" },
            new FlowParameter { Name = "Enable", DisplayName = "Enable", Type = "bool", Required = true, Description = "True to enable, false to disable measurement" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public MeasurementControlNode(MeasurementRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                var enable = context.GetNodeInput<string>(Id, "Enable") == "true";
                Service!.MeasureControl(name, enable);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Measurement control failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            var enable = context.GetNodeInput<string>(Id, "Enable") == "true";
            Console.WriteLine($"[SIMULATION] MeasurementControl: {name} enable={enable}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
