using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Simulation
{
    /// <summary>
    /// Simulated auto focus open node - no hardware dependency.
    /// </summary>
    public class SimAutoFocusOpenNode : LogicNodeBase
    {
        public override string Name { get; set; } = "AutoFocus Open (Sim)";
        public override string NodeType => "AutoFocusOpen";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether AF instance is available" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusOpen: Verify instance {instanceName}");
            await Task.Delay(200, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
