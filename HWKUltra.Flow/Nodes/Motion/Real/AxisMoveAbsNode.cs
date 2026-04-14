using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Absolute position motion node - moves axis to target position
    /// </summary>
    public class AxisMoveAbsNode : DeviceNodeBase<MotionRouter>
    {
        public override string Name { get; set; } = "Axis Move Absolute";
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

        public AxisMoveAbsNode(MotionRouter? router) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
                var position = context.GetNodeInput<double>(Id, "Position");
                var velocity = context.GetNodeInput<double>(Id, "Velocity");
                var acceleration = context.GetNodeInput<double>(Id, "Acceleration");
                var deceleration = context.GetNodeInput<double>(Id, "Deceleration");
                var waitForComplete = context.GetNodeInput<string>(Id, "WaitForComplete") != "false";

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration,
                    Dec = (float)deceleration
                };

                Service!.Move(axisName, position, profile);

                if (waitForComplete)
                    await Service.WaitForIdleAsync(axisName, 30000, context.CancellationToken);

                var actualPos = Service.GetPosition(axisName);
                context.SetNodeOutput(Id, "ActualPosition", actualPos);
                context.SetNodeOutput(Id, "CommandPosition", position);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Absolute motion failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var position = context.GetNodeInput<double>(Id, "Position");
            Console.WriteLine($"[SIMULATION] AxisMoveAbs: Moving {axisName} to {position:F3}mm");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualPosition", position);
            context.SetNodeOutput(Id, "CommandPosition", position);
            return FlowResult.Ok();
        }
    }
}
