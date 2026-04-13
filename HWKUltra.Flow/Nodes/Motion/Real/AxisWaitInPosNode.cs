using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Wait for axis in-position node - waits until axis reaches target position
    /// </summary>
    public class AxisWaitInPosNode : DeviceNodeBase<IMotionController>
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

        public AxisWaitInPosNode(IMotionController? motionController) : base(motionController) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var tolerance = context.GetVariable<double>("Tolerance");
                var timeout = context.GetVariable<int>("Timeout");
                var targetPosition = context.GetVariable<double?>("TargetPosition");

                if (IsSimulated)
                {
                    Console.WriteLine($"[AxisWaitInPos] Simulating wait for {axisName} in position");
                    await Task.Delay(100, context.CancellationToken);
                    context.SetVariable("IsInPosition", true);
                    context.SetVariable("ActualPosition", targetPosition ?? 0.0);
                    context.SetVariable("PositionError", 0.0);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                var startTime = DateTime.Now;
                bool isInPosition = false;

                // TODO: Poll axis status
                while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
                {
                    // Check if axis is busy and in position
                    // isInPosition = !Service!.IsBusy(axisName) && Service.IsInPosition(axisName, tolerance);

                    if (isInPosition)
                        break;

                    await Task.Delay(10, context.CancellationToken);
                }

                context.SetVariable("IsInPosition", isInPosition);
                context.SetVariable("ActualPosition", 0.0); // TODO: Get actual position
                context.SetVariable("PositionError", 0.0);

                return isInPosition ? FlowResult.Ok() : FlowResult.Fail($"Timeout waiting for {axisName} to reach position");
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Wait for in-position failed: {ex.Message}");
            }
        }
    }
}
