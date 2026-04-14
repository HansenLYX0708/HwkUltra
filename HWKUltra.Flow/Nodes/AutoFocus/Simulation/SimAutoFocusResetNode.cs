using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Simulation
{
    /// <summary>
    /// Simulated auto focus reset node - no hardware dependency.
    /// </summary>
    public class SimAutoFocusResetNode : LogicNodeBase
    {
        public override string Name { get; set; } = "AutoFocus Reset (Sim)";
        public override string NodeType => "AutoFocusReset";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether reset was successful" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusReset: Reset axis on {instanceName}");
            await Task.Delay(200, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
