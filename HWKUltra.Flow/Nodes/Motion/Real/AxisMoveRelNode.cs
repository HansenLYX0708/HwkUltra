using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Relative position motion node - moves axis by relative distance
    /// </summary>
    public class AxisMoveRelNode : DeviceNodeBase<IMotionController>
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

        public AxisMoveRelNode(IMotionController? motionController) : base(motionController) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var distance = context.GetVariable<double>("Distance");
                var velocity = context.GetVariable<double>("Velocity");
                var acceleration = context.GetVariable<double>("Acceleration");

                if (IsSimulated)
                {
                    Console.WriteLine($"[AxisMoveRel] Simulating relative move {axisName} by {distance:F3}mm");
                    await Task.Delay(100, context.CancellationToken);
                    context.SetVariable("ActualPosition", distance);
                    context.SetVariable("DistanceTraveled", distance);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration
                };

                // TODO: Implement relative move
                // Service!.MoveAxisRelative(axisName, distance, profile);

                context.SetVariable("ActualPosition", distance);
                context.SetVariable("DistanceTraveled", distance);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Relative motion failed: {ex.Message}");
            }
        }
    }
}
