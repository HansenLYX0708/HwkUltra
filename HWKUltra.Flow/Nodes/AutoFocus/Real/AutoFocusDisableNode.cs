using HWKUltra.AutoFocus.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Real
{
    /// <summary>
    /// Auto focus disable node - disable auto focus tracking (tracklaser 0).
    /// </summary>
    public class AutoFocusDisableNode : DeviceNodeBase<AutoFocusRouter>
    {
        public override string Name { get; set; } = "AutoFocus Disable";
        public override string NodeType => "AutoFocusDisable";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether disable was successful" }
        };

        public AutoFocusDisableNode(AutoFocusRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(instanceName))
                    return FlowResult.Fail("InstanceName is required");

                Service!.DisableAutoFocus(instanceName);
                context.SetNodeOutput(Id, "Status", true);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"AutoFocus disable failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusDisable: Disable AF on {instanceName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
