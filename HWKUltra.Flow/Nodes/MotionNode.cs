using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes
{
    /// <summary>
    /// Motion control node - controls axis movement to target position
    /// </summary>
    public class MotionNode : IFlowNode
    {
        private readonly IMotionController _motionController;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Axis Motion";
        public string NodeType => "Motion";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "Position", DisplayName = "Target Position", Type = "double", Required = true, Description = "Target position (mm)" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 50000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Acceleration" },
            new FlowParameter { Name = "Deceleration", DisplayName = "Deceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Deceleration" }
        };

        public List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualPosition", DisplayName = "Actual Position", Type = "double", Description = "Actual position after motion complete" }
        };

        public MotionNode(IMotionController motionController)
        {
            _motionController = motionController;
        }

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                // Get parameters from context
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var position = context.GetVariable<double>("Position");
                var velocity = context.GetVariable<double>("Velocity");
                var acceleration = context.GetVariable<double>("Acceleration");
                var deceleration = context.GetVariable<double>("Deceleration");

                if (string.IsNullOrEmpty(axisName))
                    return FlowResult.Fail("Axis name cannot be empty");

                // Build motion profile
                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration,
                    Dec = (float)deceleration
                };

                // Execute motion
                _motionController.MoveAxis(axisName, position, profile);

                // Record result
                context.SetVariable("ActualPosition", position);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Motion execution failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Multi-axis interpolation motion node
    /// </summary>
    public class MotionGroupNode : IFlowNode
    {
        private readonly IMotionController _motionController;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Multi-axis Interpolation";
        public string NodeType => "MotionGroup";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "GroupName", DisplayName = "Group Name", Type = "string", Required = true, Description = "e.g., XY, XYZ" },
            new FlowParameter { Name = "Positions", DisplayName = "Position Data", Type = "position", Required = true, Description = "Target positions for each axis" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 30000.0 },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 500000.0 }
        };

        public List<FlowParameter> Outputs { get; } = new();

        public MotionGroupNode(IMotionController motionController)
        {
            _motionController = motionController;
        }

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var groupName = context.GetVariable<string>("GroupName") ?? "XY";
                var positions = context.GetVariable<AxisPosition>("Positions");
                var velocity = context.GetVariable<double>("Velocity");
                var acceleration = context.GetVariable<double>("Acceleration");

                if (positions == null)
                    return FlowResult.Fail("Position data cannot be empty");

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration
                };

                _motionController.MoveGroup(groupName, positions, profile);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Interpolation motion failed: {ex.Message}");
            }
        }
    }
}
