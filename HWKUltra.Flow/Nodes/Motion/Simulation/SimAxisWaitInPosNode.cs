using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulation
{
    /// <summary>
    /// Simulated wait for axis in-position node - no hardware dependency.
    /// </summary>
    public class SimAxisWaitInPosNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Axis Wait In Position (Sim)";
        public override string NodeType => "AxisWaitInPos";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "TargetPosition", DisplayName = "Target Position", Type = "double", Required = false, Description = "Expected position (optional)" },
            new FlowParameter { Name = "Tolerance", DisplayName = "Tolerance", Type = "double", Required = false, DefaultValue = 0.01, Description = "Position tolerance (mm)" },
            new FlowParameter { Name = "Timeout", DisplayName = "Timeout", Type = "int", Required = false, DefaultValue = 30000, Description = "Timeout in milliseconds" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "IsInPosition", DisplayName = "Is In Position", Type = "bool", Description = "Whether axis is in position" },
            new FlowParameter { Name = "ActualPosition", DisplayName = "Actual Position", Type = "double", Description = "Actual position" },
            new FlowParameter { Name = "PositionError", DisplayName = "Position Error", Type = "double", Description = "Position error from target" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var targetPosition = context.GetNodeInput<double>(Id, "TargetPosition");
            Console.WriteLine($"[SIMULATION] AxisWaitInPos: Waiting for {axisName} at {targetPosition:F3}mm");
            await Task.Delay(100, context.CancellationToken);
            context.SetNodeOutput(Id, "IsInPosition", true);
            context.SetNodeOutput(Id, "ActualPosition", targetPosition);
            context.SetNodeOutput(Id, "PositionError", 0.0);
            return FlowResult.Ok();
        }
    }
}
