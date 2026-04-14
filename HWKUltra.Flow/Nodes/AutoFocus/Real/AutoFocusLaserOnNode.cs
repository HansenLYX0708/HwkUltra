using HWKUltra.AutoFocus.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Real
{
    /// <summary>
    /// Auto focus laser on node - turn laser on (lasergate 1).
    /// </summary>
    public class AutoFocusLaserOnNode : DeviceNodeBase<AutoFocusRouter>
    {
        public override string Name { get; set; } = "AutoFocus Laser On";
        public override string NodeType => "AutoFocusLaserOn";
        protected override int SimulatedDelayMs => 30;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether laser on was successful" }
        };

        public AutoFocusLaserOnNode(AutoFocusRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(instanceName))
                    return FlowResult.Fail("InstanceName is required");

                Service!.LaserOn(instanceName);
                context.SetNodeOutput(Id, "Status", true);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"AutoFocus laser on failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusLaserOn: Laser on for {instanceName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
