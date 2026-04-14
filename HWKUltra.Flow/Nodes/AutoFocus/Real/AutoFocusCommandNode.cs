using HWKUltra.AutoFocus.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Real
{
    /// <summary>
    /// Auto focus command node - send a custom LAF command.
    /// </summary>
    public class AutoFocusCommandNode : DeviceNodeBase<AutoFocusRouter>
    {
        public override string Name { get; set; } = "AutoFocus Command";
        public override string NodeType => "AutoFocusCommand";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" },
            new FlowParameter { Name = "Command", DisplayName = "Command", Type = "string", Required = true, Description = "Raw LAF command string" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Response", DisplayName = "Response", Type = "string", Description = "Response from the LAF controller" }
        };

        public AutoFocusCommandNode(AutoFocusRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                var command = context.GetNodeInput<string>(Id, "Command") ?? "";

                if (string.IsNullOrEmpty(instanceName))
                    return FlowResult.Fail("InstanceName is required");
                if (string.IsNullOrEmpty(command))
                    return FlowResult.Fail("Command is required");

                var response = Service!.SendCommand(instanceName, command);
                context.SetNodeOutput(Id, "Response", response);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"AutoFocus command failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            var command = context.GetNodeInput<string>(Id, "Command") ?? "unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusCommand: Send '{command}' to {instanceName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Response", "OK (simulated)");
            return FlowResult.Ok();
        }
    }
}
