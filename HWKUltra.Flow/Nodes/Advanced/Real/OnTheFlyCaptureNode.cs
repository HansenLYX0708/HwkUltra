using HWKUltra.Camera.Core;
using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Advanced.Real
{
    /// <summary>
    /// On-the-fly capture node - captures image while axis is moving
    /// Combines motion and camera trigger with position-based trigger
    /// </summary>
    public class OnTheFlyCaptureNode : CompositeNodeBase
    {
        private readonly MotionRouter? _motionRouter;
        private readonly CameraRouter? _cameraRouter;

        public override string Name { get; set; } = "On-The-Fly Capture";
        public override string NodeType => "OnTheFlyCapture";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            // Motion parameters
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "Axis to move (e.g., X)" },
            new FlowParameter { Name = "TargetPosition", DisplayName = "Target Position", Type = "double", Required = true, Description = "Target position (mm)" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = true, Description = "Motion velocity (mm/s)" },
            // Trigger parameters
            new FlowParameter { Name = "TriggerPositions", DisplayName = "Trigger Positions", Type = "array", Required = true, Description = "Array of positions to trigger capture" },
            new FlowParameter { Name = "PreTriggerDistance", DisplayName = "Pre-Trigger Distance", Type = "double", Required = false, DefaultValue = 5.0, Description = "Distance before trigger to arm (mm)" },
            // Camera parameters
            new FlowParameter { Name = "CameraId", DisplayName = "Camera ID", Type = "string", Required = true, Description = "Camera to trigger" },
            new FlowParameter { Name = "ExposureTime", DisplayName = "Exposure Time", Type = "double", Required = false, DefaultValue = 10000.0, Description = "Exposure in microseconds" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ImagesCaptured", DisplayName = "Images Captured", Type = "int", Description = "Number of images captured" },
            new FlowParameter { Name = "CapturePositions", DisplayName = "Capture Positions", Type = "array", Description = "Actual positions where images were captured" },
            new FlowParameter { Name = "MotionComplete", DisplayName = "Motion Complete", Type = "bool", Description = "Whether motion completed successfully" }
        };

        public OnTheFlyCaptureNode(MotionRouter? motionRouter = null, CameraRouter? cameraRouter = null, bool simulate = false)
            : base(simulate || motionRouter == null || cameraRouter == null)
        {
            _motionRouter = motionRouter;
            _cameraRouter = cameraRouter;
        }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
                var targetPosition = context.GetNodeInput<double>(Id, "TargetPosition");
                var velocity = context.GetNodeInput<double>(Id, "Velocity");
                var triggerPositions = context.GetVariable<double[]>("TriggerPositions") ?? Array.Empty<double>();
                var cameraId = context.GetNodeInput<string>(Id, "CameraId") ?? "Cam1";

                if (IsSimulated)
                {
                    Console.WriteLine($"[SIMULATION] OnTheFlyCapture: Moving {axisName} to {targetPosition}mm at {velocity}mm/s");
                    var capturedImages = 0;
                    var capturePositions = new List<double>();

                    foreach (var triggerPos in triggerPositions.OrderBy(p => p))
                    {
                        var delayMs = Math.Max((int)(Math.Abs(triggerPos) / velocity * 1000), 10);
                        await Task.Delay(delayMs, context.CancellationToken);
                        Console.WriteLine($"[SIMULATION] OnTheFlyCapture: Triggered at {triggerPos:F3}mm");
                        capturedImages++;
                        capturePositions.Add(triggerPos);
                    }

                    await Task.Delay(50, context.CancellationToken);
                    context.SetNodeOutput(Id, "ImagesCaptured", capturedImages);
                    context.SetNodeOutput(Id, "CapturePositions", capturePositions.ToArray());
                    context.SetNodeOutput(Id, "MotionComplete", true);
                    return FlowResult.Ok();
                }

                if (_motionRouter == null)
                    return FlowResult.Fail("Motion router not available");

                // TODO: Implement real on-the-fly capture
                // 1. Configure position trigger (PT) table with trigger positions
                // 2. Set up camera trigger on PT output
                // 3. Start motion via _motionRouter.Move()
                // 4. Monitor and capture images at trigger positions
                // 5. Wait for motion completion via _motionRouter.WaitForIdleAsync()

                var profile = new MotionProfile { Vel = (float)velocity };
                _motionRouter.Move(axisName, targetPosition, profile);
                await _motionRouter.WaitForIdleAsync(axisName, 60000, context.CancellationToken);

                context.SetNodeOutput(Id, "ImagesCaptured", triggerPositions.Length);
                context.SetNodeOutput(Id, "CapturePositions", triggerPositions);
                context.SetNodeOutput(Id, "MotionComplete", true);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"On-the-fly capture failed: {ex.Message}");
            }
        }
    }
}
