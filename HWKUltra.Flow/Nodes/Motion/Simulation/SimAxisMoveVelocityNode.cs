using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulation
{
    /// <summary>
    /// Simulated velocity motion node - no hardware dependency.
    /// </summary>
    public class SimAxisMoveVelocityNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Axis Move Velocity (Sim)";
        public override string NodeType => "AxisMoveVelocity";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = true, Description = "Target velocity (positive/negative for direction)" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Acceleration" },
            new FlowParameter { Name = "Duration", DisplayName = "Duration", Type = "double", Required = false, DefaultValue = 0.0, Description = "Duration in ms (0 = continuous)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualVelocity", DisplayName = "Actual Velocity", Type = "double", Description = "Actual velocity achieved" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var velocity = context.GetNodeInput<double>(Id, "Velocity");
            var duration = context.GetNodeInput<double>(Id, "Duration");
            Console.WriteLine($"[SIMULATION] AxisMoveVelocity: {axisName} at {velocity:F1}mm/s");
            if (duration > 0)
                await Task.Delay((int)duration, context.CancellationToken);
            else
                await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualVelocity", velocity);
            return FlowResult.Ok();
        }
    }
}
