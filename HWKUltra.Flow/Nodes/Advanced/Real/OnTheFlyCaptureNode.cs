using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Advanced.Real
{
    /// <summary>
    /// On-the-fly capture node - captures image while axis is moving
    /// Combines motion and camera trigger with position-based trigger
    /// </summary>
    public class OnTheFlyCaptureNode : CompositeNodeBase
    {
        private readonly IMotionController? _motionController;
        private readonly object? _cameraService;  // TODO: Replace with ICameraService

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

        public OnTheFlyCaptureNode(IMotionController? motionController = null, object? cameraService = null, bool simulate = false)
            : base(simulate || motionController == null || cameraService == null)
        {
            _motionController = motionController;
            _cameraService = cameraService;
        }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var targetPosition = context.GetVariable<double>("TargetPosition");
                var velocity = context.GetVariable<double>("Velocity");
                var triggerPositions = context.GetVariable<double[]>("TriggerPositions") ?? Array.Empty<double>();
                var preTriggerDist = context.GetVariable<double>("PreTriggerDistance");
                var cameraId = context.GetVariable<string>("CameraId") ?? "Cam1";
                var exposureTime = context.GetVariable<double>("ExposureTime");

                if (IsSimulated)
                {
                    return await ExecuteSimulatedAsync(context, axisName, targetPosition, velocity, triggerPositions, cameraId);
                }

                return await ExecuteRealAsync(context, axisName, targetPosition, velocity, triggerPositions, preTriggerDist, cameraId, exposureTime);
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"On-the-fly capture failed: {ex.Message}");
            }
        }

        private async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context, string axisName, double targetPosition, double velocity, double[] triggerPositions, string cameraId)
        {
            Console.WriteLine($"[OnTheFlyCapture] SIMULATION: Moving {axisName} to {targetPosition}mm at {velocity}mm/s");
            Console.WriteLine($"[OnTheFlyCapture] SIMULATION: Will trigger at positions: {string.Join(", ", triggerPositions)}");

            var capturedImages = 0;
            var capturePositions = new List<double>();

            foreach (var triggerPos in triggerPositions.OrderBy(p => p))
            {
                // Simulate reaching trigger position
                var delayMs = (int)(Math.Abs(triggerPos) / velocity * 1000);
                await Task.Delay(delayMs, context.CancellationToken);

                Console.WriteLine($"[OnTheFlyCapture] SIMULATION: Triggered capture at position {triggerPos:F3}mm");
                capturedImages++;
                capturePositions.Add(triggerPos);
            }

            // Complete motion to target
            await Task.Delay(50, context.CancellationToken);

            context.SetVariable("ImagesCaptured", capturedImages);
            context.SetVariable("CapturePositions", capturePositions.ToArray());
            context.SetVariable("MotionComplete", true);

            Console.WriteLine($"[OnTheFlyCapture] SIMULATION: Captured {capturedImages} images, motion complete");

            return FlowResult.Ok();
        }

        private async Task<FlowResult> ExecuteRealAsync(FlowContext context, string axisName, double targetPosition, double velocity, double[] triggerPositions, double preTriggerDist, string cameraId, double exposureTime)
        {
            if (_motionController == null)
                return FlowResult.Fail("Motion controller not available");

            var capturedImages = 0;
            var capturePositions = new List<double>();

            // TODO: Implement real on-the-fly capture
            // 1. Configure position trigger (PT) table with trigger positions
            // 2. Set up camera trigger on PT output
            // 3. Start motion
            // 4. Monitor and capture images at trigger positions
            // 5. Wait for motion completion

            Console.WriteLine($"[OnTheFlyCapture] Real capture: Moving {axisName} with {triggerPositions.Length} trigger points");

            // Placeholder for actual implementation
            await Task.Delay(100, context.CancellationToken);

            context.SetVariable("ImagesCaptured", triggerPositions.Length);
            context.SetVariable("CapturePositions", triggerPositions);
            context.SetVariable("MotionComplete", true);

            return FlowResult.Ok();
        }
    }
}
