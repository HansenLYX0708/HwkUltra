using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Wait for axis in-position node - waits until axis reaches target position
    /// </summary>
    public class AxisWaitInPosNode : DeviceNodeBase<MotionRouter>
    {
        public override string Name { get; set; } = "Axis Wait In Position";
        public override string NodeType => "AxisWaitInPos";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "TargetPosition", DisplayName = "Target Position", Type = "double", Required = false, Description = "Expected position (optional)" },
            new FlowParameter { Name = "Tolerance", DisplayName = "Tolerance", Type = "double", Required = false, DefaultValue = 0.01, Description = "Position tolerance (mm)" },
            new FlowParameter { Name = "Timeout", DisplayName = "Timeout", Type = "int", Required = false, DefaultValue = 30000, Description = "Timeout in milliseconds" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "IsInPosition", DisplayName = "Is In Position", Type = "bool", Description = "Whether axis is in position" },
            new FlowParameter { Name = "ActualPosition", DisplayName = "Actual Position", Type = "double", Description = "Actual position" },
            new FlowParameter { Name = "PositionError", DisplayName = "Position Error", Type = "double", Description = "Position error from target" }
        };

        public AxisWaitInPosNode(MotionRouter? router) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
                var tolerance = context.GetNodeInput<double>(Id, "Tolerance");
                var timeout = context.GetNodeInput<int>(Id, "Timeout");
                if (timeout <= 0) timeout = 30000;
                var targetPosition = context.GetNodeInput<double>(Id, "TargetPosition");

                await Service!.WaitForIdleAsync(axisName, timeout, context.CancellationToken);

                var actualPos = Service.GetPosition(axisName);
                var posError = Math.Abs(actualPos - targetPosition);
                var inPos = posError <= (tolerance > 0 ? tolerance : 0.01);

                context.SetNodeOutput(Id, "IsInPosition", inPos);
                context.SetNodeOutput(Id, "ActualPosition", actualPos);
                context.SetNodeOutput(Id, "PositionError", posError);

                return inPos ? FlowResult.Ok() : FlowResult.Fail($"Axis {axisName} position error {posError:F4}mm exceeds tolerance {tolerance:F4}mm");
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Wait for in-position failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            var targetPosition = context.GetNodeInput<double>(Id, "TargetPosition");
            Console.WriteLine($"[SIMULATION] AxisWaitInPos: Waiting for {axisName} at {targetPosition:F3}mm");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "IsInPosition", true);
            context.SetNodeOutput(Id, "ActualPosition", targetPosition);
            context.SetNodeOutput(Id, "PositionError", 0.0);
            return FlowResult.Ok();
        }
    }
}
