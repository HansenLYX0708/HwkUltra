using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Simulation
{
    /// <summary>
    /// Simulated auto focus laser off node - no hardware dependency.
    /// </summary>
    public class SimAutoFocusLaserOffNode : LogicNodeBase
    {
        public override string Name { get; set; } = "AutoFocus Laser Off (Sim)";
        public override string NodeType => "AutoFocusLaserOff";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether laser off was successful" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusLaserOff: Laser off for {instanceName}");
            await Task.Delay(30, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
