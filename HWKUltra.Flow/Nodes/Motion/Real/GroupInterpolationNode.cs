using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Multi-axis interpolation motion node - coordinated motion of multiple axes
    /// </summary>
    public class GroupInterpolationNode : DeviceNodeBase<IMotionController>
    {
        public override string Name { get; set; } = "Group Interpolation";
        public override string NodeType => "GroupInterpolation";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "GroupName", DisplayName = "Group Name", Type = "string", Required = true, Description = "e.g., XY, XYZ" },
            new FlowParameter { Name = "Positions", DisplayName = "Positions", Type = "position", Required = true, Description = "Target positions for each axis" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 30000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 500000.0, Description = "Acceleration" },
            new FlowParameter { Name = "PathType", DisplayName = "Path Type", Type = "string", Required = false, DefaultValue = "Linear", Description = "Linear, Arc, Spline" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "CompletionStatus", DisplayName = "Completion Status", Type = "bool", Description = "Whether motion completed successfully" }
        };

        public GroupInterpolationNode(IMotionController? motionController) : base(motionController) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var groupName = context.GetVariable<string>("GroupName") ?? "XY";
                var positions = context.GetVariable<AxisPosition>("Positions");
                var velocity = context.GetVariable<double>("Velocity");
                var acceleration = context.GetVariable<double>("Acceleration");
                var pathType = context.GetVariable<string>("PathType") ?? "Linear";

                if (positions == null)
                    return FlowResult.Fail("Position data cannot be empty");

                if (IsSimulated)
                {
                    Console.WriteLine($"[GroupInterpolation] Simulating {pathType} motion for group {groupName}");
                    await Task.Delay(200, context.CancellationToken);
                    context.SetVariable("CompletionStatus", true);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration
                };

                Service!.MoveGroup(groupName, positions, profile);

                context.SetVariable("CompletionStatus", true);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Group interpolation failed: {ex.Message}");
            }
        }
    }
}
