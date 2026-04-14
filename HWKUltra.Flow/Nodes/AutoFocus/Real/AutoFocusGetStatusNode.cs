using HWKUltra.AutoFocus.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Real
{
    /// <summary>
    /// Auto focus get status node - get focus value and motor position.
    /// </summary>
    public class AutoFocusGetStatusNode : DeviceNodeBase<AutoFocusRouter>
    {
        public override string Name { get; set; } = "AutoFocus Get Status";
        public override string NodeType => "AutoFocusGetStatus";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "FocusValue", DisplayName = "Focus Value", Type = "double", Description = "Current focus value" },
            new FlowParameter { Name = "MotorPosition", DisplayName = "Motor Position", Type = "double", Description = "Current motor position" }
        };

        public AutoFocusGetStatusNode(AutoFocusRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(instanceName))
                    return FlowResult.Fail("InstanceName is required");

                var focusValue = Service!.GetFocusValue(instanceName);
                var motorPosition = Service.GetMotorPosition(instanceName);

                context.SetNodeOutput(Id, "FocusValue", focusValue);
                context.SetNodeOutput(Id, "MotorPosition", motorPosition);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"AutoFocus get status failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusGetStatus: Get status from {instanceName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "FocusValue", 0.0);
            context.SetNodeOutput(Id, "MotorPosition", 0.0);
            return FlowResult.Ok();
        }
    }
}
