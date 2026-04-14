using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulation
{
    /// <summary>
    /// Simulated absolute position motion node - no hardware dependency.
    /// </summary>
    public class SimAxisMoveAbsNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Axis Move Absolute (Sim)";
        public override string NodeType => "AxisMoveAbs";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "Position", DisplayName = "Target Position", Type = "double", Required = true, Description = "Target position (mm)" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 50000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Acceleration" },
            new FlowParameter { Name = "Deceleration", DisplayName = "Deceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Deceleration" },
            new FlowParameter { Name = "WaitForComplete", DisplayName = "Wait For Complete", Type = "bool", Required = false, DefaultValue = true, Description = "Wait for motion to complete" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualPosition", DisplayName = "Actual Position", Type = "double", Description = "Actual position after motion" },
            new FlowParameter { Name = "CommandPosition", DisplayName = "Command Position", Type = "double", Description = "Commanded position" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var position = context.GetNodeInput<double>(Id, "Position");
            Console.WriteLine($"[SIMULATION] AxisMoveAbs: Moving {axisName} to {position:F3}mm");
            await Task.Delay(100, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualPosition", position);
            context.SetNodeOutput(Id, "CommandPosition", position);
            return FlowResult.Ok();
        }
    }
}
