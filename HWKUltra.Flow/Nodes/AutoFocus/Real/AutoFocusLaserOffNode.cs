using HWKUltra.AutoFocus.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Real
{
    /// <summary>
    /// Auto focus laser off node - turn laser off (lasergate 0).
    /// </summary>
    public class AutoFocusLaserOffNode : DeviceNodeBase<AutoFocusRouter>
    {
        public override string Name { get; set; } = "AutoFocus Laser Off";
        public override string NodeType => "AutoFocusLaserOff";
        protected override int SimulatedDelayMs => 30;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether laser off was successful" }
        };

        public AutoFocusLaserOffNode(AutoFocusRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(instanceName))
                    return FlowResult.Fail("InstanceName is required");

                Service!.LaserOff(instanceName);
                context.SetNodeOutput(Id, "Status", true);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"AutoFocus laser off failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusLaserOff: Laser off for {instanceName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
