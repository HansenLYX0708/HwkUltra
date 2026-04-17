using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.TeachData.Real
{
    /// <summary>
    /// Moves all axes to a named teach position using group interpolation.
    /// Requires both MotionRouter and TeachDataService.
    /// </summary>
    public class MoveToTeachPositionNode : DeviceNodeBase<MotionRouter>
    {
        private readonly TeachDataService? _teachData;

        public override string Name { get; set; } = "Move To Teach Position";
        public override string NodeType => "MoveToTeachPosition";
        protected override int SimulatedDelayMs => 200;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Required = true, Description = "Teach position name (e.g., BarcodeScannerPos)" },
            new FlowParameter { Name = "GroupName", DisplayName = "Motion Group", Type = "string", Required = false, DefaultValue = "XYZ", Description = "Motion axis group name" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 30000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 500000.0, Description = "Acceleration" },
            new FlowParameter { Name = "WaitForComplete", DisplayName = "Wait For Complete", Type = "bool", Required = false, DefaultValue = true, Description = "Wait for motion to complete" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Description = "The position that was moved to" },
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool", Description = "Whether the move completed successfully" }
        };

        public MoveToTeachPositionNode(MotionRouter? router, TeachDataService? teachData) : base(router)
        {
            _teachData = teachData;
        }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var posName = context.GetNodeInput<string>(Id, "PositionName")
                    ?? throw new ArgumentException("PositionName is required");
                var groupName = context.GetNodeInput<string>(Id, "GroupName") ?? "XYZ";
                var velocity = context.GetNodeInput<double>(Id, "Velocity");
                var acceleration = context.GetNodeInput<double>(Id, "Acceleration");
                var waitForComplete = context.GetNodeInput<string>(Id, "WaitForComplete") != "false";

                if (_teachData == null)
                    return FlowResult.Fail("TeachDataService not available");

                var axisPos = _teachData.GetAxisPosition(posName);

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration
                };

                Service!.MoveGroup(groupName, axisPos, profile);

                if (waitForComplete)
                {
                    // Wait for each axis in the position to reach target
                    foreach (var axis in axisPos.Values.Keys)
                    {
                        await Service.WaitForIdleAsync(axis, 30000, context.CancellationToken);
                    }
                }

                context.SetNodeOutput(Id, "PositionName", posName);
                context.SetNodeOutput(Id, "Success", true);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                context.SetNodeOutput(Id, "Success", false);
                return FlowResult.Fail($"MoveToTeachPosition failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var posName = context.GetNodeInput<string>(Id, "PositionName") ?? "(unknown)";

            if (_teachData != null && _teachData.TryGetAxisPosition(posName, out var pos))
            {
                Console.WriteLine($"[SIMULATION] MoveToTeachPosition: Moving to '{posName}' = {pos}");
            }
            else
            {
                Console.WriteLine($"[SIMULATION] MoveToTeachPosition: Moving to '{posName}' (no teach data loaded)");
            }

            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "PositionName", posName);
            context.SetNodeOutput(Id, "Success", true);
            return FlowResult.Ok();
        }
    }
}
