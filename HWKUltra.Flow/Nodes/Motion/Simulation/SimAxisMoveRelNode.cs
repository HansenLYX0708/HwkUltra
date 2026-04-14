using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulation
{
    /// <summary>
    /// Simulated relative position motion node - no hardware dependency.
    /// </summary>
    public class SimAxisMoveRelNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Axis Move Relative (Sim)";
        public override string NodeType => "AxisMoveRel";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "Distance", DisplayName = "Distance", Type = "double", Required = true, Description = "Relative distance (mm)" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 50000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Acceleration" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualPosition", DisplayName = "Actual Position", Type = "double", Description = "Actual position after motion" },
            new FlowParameter { Name = "DistanceTraveled", DisplayName = "Distance Traveled", Type = "double", Description = "Actual distance moved" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var distance = context.GetNodeInput<double>(Id, "Distance");
            Console.WriteLine($"[SIMULATION] AxisMoveRel: Moving {axisName} by {distance:F3}mm");
            await Task.Delay(100, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualPosition", distance);
            context.SetNodeOutput(Id, "DistanceTraveled", distance);
            return FlowResult.Ok();
        }
    }
}
