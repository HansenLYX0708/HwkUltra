using HWKUltra.AutoFocus.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Real
{
    /// <summary>
    /// Auto focus open node - verify AF instance availability.
    /// </summary>
    public class AutoFocusOpenNode : DeviceNodeBase<AutoFocusRouter>
    {
        public override string Name { get; set; } = "AutoFocus Open";
        public override string NodeType => "AutoFocusOpen";
        protected override int SimulatedDelayMs => 200;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name (e.g., MainAF)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether AF instance is available" }
        };

        public AutoFocusOpenNode(AutoFocusRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(instanceName))
                    return FlowResult.Fail("InstanceName is required");

                var available = Service!.HasInstance(instanceName);
                context.SetNodeOutput(Id, "Status", available);
                return available ? FlowResult.Ok() : FlowResult.Fail($"AF instance '{instanceName}' not found");
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"AutoFocus open failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusOpen: Verify instance {instanceName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
