using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Velocity motion node - moves axis at constant velocity
    /// </summary>
    public class AxisMoveVelocityNode : DeviceNodeBase<IMotionController>
    {
        public override string Name { get; set; } = "Axis Move Velocity";
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

        public AxisMoveVelocityNode(IMotionController? motionController) : base(motionController) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var velocity = context.GetVariable<double>("Velocity");
                var acceleration = context.GetVariable<double>("Acceleration");
                var duration = context.GetVariable<double>("Duration");

                if (IsSimulated)
                {
                    Console.WriteLine($"[AxisMoveVelocity] Simulating velocity move {axisName} at {velocity:F1} mm/s");
                    if (duration > 0)
                    {
                        await Task.Delay((int)duration, context.CancellationToken);
                    }
                    context.SetVariable("ActualVelocity", velocity);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                var profile = new MotionProfile
                {
                    Vel = (float)Math.Abs(velocity),
                    Acc = (float)acceleration
                };

                // TODO: Implement velocity move
                // Service!.MoveAxisVelocity(axisName, velocity, profile);

                if (duration > 0)
                {
                    await Task.Delay((int)duration, context.CancellationToken);
                    // TODO: Stop axis after duration
                }

                context.SetVariable("ActualVelocity", velocity);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Velocity motion failed: {ex.Message}");
            }
        }
    }
}
