using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Simulation
{
    /// <summary>
    /// Simulated auto focus command node - no hardware dependency.
    /// </summary>
    public class SimAutoFocusCommandNode : LogicNodeBase
    {
        public override string Name { get; set; } = "AutoFocus Command (Sim)";
        public override string NodeType => "AutoFocusCommand";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" },
            new FlowParameter { Name = "Command", DisplayName = "Command", Type = "string", Required = true, Description = "Raw LAF command string" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Response", DisplayName = "Response", Type = "string", Description = "Response from the LAF controller" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            var command = context.GetNodeInput<string>(Id, "Command") ?? "unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusCommand: Send '{command}' to {instanceName}");
            await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "Response", "OK (simulated)");
            return FlowResult.Ok();
        }
    }
}
