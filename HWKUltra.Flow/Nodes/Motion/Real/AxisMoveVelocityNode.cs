using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Velocity motion node - moves axis at constant velocity
    /// </summary>
    public class AxisMoveVelocityNode : DeviceNodeBase<MotionRouter>
    {
        public override string Name { get; set; } = "Axis Move Velocity";
        public override string NodeType => "AxisMoveVelocity";
        protected override int SimulatedDelayMs => 50;

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

        public AxisMoveVelocityNode(MotionRouter? router) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
                var velocity = context.GetNodeInput<double>(Id, "Velocity");
                var acceleration = context.GetNodeInput<double>(Id, "Acceleration");
                var duration = context.GetNodeInput<double>(Id, "Duration");

                var profile = new MotionProfile
                {
                    Vel = (float)Math.Abs(velocity),
                    Acc = (float)acceleration
                };

                Service!.MoveVelocity(axisName, velocity, profile);

                if (duration > 0)
                {
                    await Task.Delay((int)duration, context.CancellationToken);
                    Service.Stop(axisName);
                }

                context.SetNodeOutput(Id, "ActualVelocity", velocity);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Velocity motion failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var velocity = context.GetNodeInput<double>(Id, "Velocity");
            var duration = context.GetNodeInput<double>(Id, "Duration");
            Console.WriteLine($"[SIMULATION] AxisMoveVelocity: {axisName} at {velocity:F1}mm/s");
            if (duration > 0)
                await Task.Delay((int)duration, context.CancellationToken);
            else
                await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualVelocity", velocity);
            return FlowResult.Ok();
        }
    }
}
