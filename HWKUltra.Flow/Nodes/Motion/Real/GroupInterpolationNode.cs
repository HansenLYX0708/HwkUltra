using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Multi-axis interpolation motion node - coordinated motion of multiple axes
    /// </summary>
    public class GroupInterpolationNode : DeviceNodeBase<MotionRouter>
    {
        public override string Name { get; set; } = "Group Interpolation";
        public override string NodeType => "GroupInterpolation";
        protected override int SimulatedDelayMs => 200;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "GroupName", DisplayName = "Group Name", Type = "string", Required = true, Description = "e.g., XY, XYZ" },
            new FlowParameter { Name = "X", DisplayName = "X Position", Type = "double", Required = false, Description = "X axis target position" },
            new FlowParameter { Name = "Y", DisplayName = "Y Position", Type = "double", Required = false, Description = "Y axis target position" },
            new FlowParameter { Name = "Z", DisplayName = "Z Position", Type = "double", Required = false, Description = "Z axis target position" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 30000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 500000.0, Description = "Acceleration" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "CompletionStatus", DisplayName = "Completion Status", Type = "bool", Description = "Whether motion completed successfully" }
        };

        public GroupInterpolationNode(MotionRouter? router) : base(router) { }

        protected override Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var groupName = context.GetNodeInput<string>(Id, "GroupName") ?? "XY";
                var velocity = context.GetNodeInput<double>(Id, "Velocity");
                var acceleration = context.GetNodeInput<double>(Id, "Acceleration");

                // Build AxisPosition from individual axis properties
                var posDict = new Dictionary<string, double>();
                var x = context.GetNodeInput<double>(Id, "X");
                var y = context.GetNodeInput<double>(Id, "Y");
                var z = context.GetNodeInput<double>(Id, "Z");
                if (x != 0) posDict["X"] = x;
                if (y != 0) posDict["Y"] = y;
                if (z != 0) posDict["Z"] = z;

                if (posDict.Count == 0)
                    return Task.FromResult(FlowResult.Fail("No axis position data provided"));

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration
                };

                Service!.MoveGroup(groupName, posDict, profile);

                context.SetNodeOutput(Id, "CompletionStatus", true);
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"Group interpolation failed: {ex.Message}"));
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var groupName = context.GetNodeInput<string>(Id, "GroupName") ?? "XY";
            Console.WriteLine($"[SIMULATION] GroupInterpolation: Moving group {groupName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "CompletionStatus", true);
            return FlowResult.Ok();
        }
    }
}
