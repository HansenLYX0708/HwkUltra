using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Relative position motion node - moves axis by relative distance
    /// </summary>
    public class AxisMoveRelNode : DeviceNodeBase<MotionRouter>
    {
        public override string Name { get; set; } = "Axis Move Relative";
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

        public AxisMoveRelNode(MotionRouter? router) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
                var distance = context.GetNodeInput<double>(Id, "Distance");
                var velocity = context.GetNodeInput<double>(Id, "Velocity");
                var acceleration = context.GetNodeInput<double>(Id, "Acceleration");

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration
                };

                var posBefore = Service!.GetPosition(axisName);
                Service.MoveRelative(axisName, distance, profile);
                await Service.WaitForIdleAsync(axisName, 30000, context.CancellationToken);

                var posAfter = Service.GetPosition(axisName);
                context.SetNodeOutput(Id, "ActualPosition", posAfter);
                context.SetNodeOutput(Id, "DistanceTraveled", posAfter - posBefore);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Relative motion failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var distance = context.GetNodeInput<double>(Id, "Distance");
            Console.WriteLine($"[SIMULATION] AxisMoveRel: Moving {axisName} by {distance:F3}mm");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualPosition", distance);
            context.SetNodeOutput(Id, "DistanceTraveled", distance);
            return FlowResult.Ok();
        }
    }
}
