using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.AutoFocus.Simulation
{
    /// <summary>
    /// Simulated auto focus get status node - no hardware dependency.
    /// </summary>
    public class SimAutoFocusGetStatusNode : LogicNodeBase
    {
        public override string Name { get; set; } = "AutoFocus Get Status (Sim)";
        public override string NodeType => "AutoFocusGetStatus";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Logical AF instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "FocusValue", DisplayName = "Focus Value", Type = "double", Description = "Current focus value" },
            new FlowParameter { Name = "MotorPosition", DisplayName = "Motor Position", Type = "double", Description = "Current motor position" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var instanceName = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] AutoFocusGetStatus: Get status from {instanceName}");
            await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "FocusValue", 0.0);
            context.SetNodeOutput(Id, "MotorPosition", 0.0);
            return FlowResult.Ok();
        }
    }
}
