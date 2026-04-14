using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulation
{
    /// <summary>
    /// Simulated axis homing node - no hardware dependency.
    /// </summary>
    public class SimAxisHomeNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Axis Home (Sim)";
        public override string NodeType => "AxisHome";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "HomeMode", DisplayName = "Home Mode", Type = "string", Required = false, DefaultValue = "Auto", Description = "Auto, Positive, Negative" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 10000.0, Description = "Homing velocity" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "HomeCompleted", DisplayName = "Home Completed", Type = "bool", Description = "Whether homing was successful" },
            new FlowParameter { Name = "HomePosition", DisplayName = "Home Position", Type = "double", Description = "Position after homing" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            Console.WriteLine($"[SIMULATION] AxisHome: Homing axis {axisName}");
            await Task.Delay(500, context.CancellationToken);
            context.SetNodeOutput(Id, "HomeCompleted", true);
            context.SetNodeOutput(Id, "HomePosition", 0.0);
            return FlowResult.Ok();
        }
    }
}
