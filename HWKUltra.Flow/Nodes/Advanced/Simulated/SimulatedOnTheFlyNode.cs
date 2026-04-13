using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Advanced.Simulated
{
    /// <summary>
    /// Simulated on-the-fly capture node - for testing fly-capture without hardware
    /// </summary>
    public class SimulatedOnTheFlyNode : LogicNodeBase, ISimulatedNode
    {
        public override string Name { get; set; } = "Simulated On-The-Fly Capture";
        public override string NodeType => "OnTheFlyCapture";

        public bool SimulateExecution { get; set; } = true;
        public int SimulatedDelayMs { get; set; } = 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "Axis to move" },
            new FlowParameter { Name = "TargetPosition", DisplayName = "Target Position", Type = "double", Required = true, Description = "Target position (mm)" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = true, Description = "Motion velocity" },
            new FlowParameter { Name = "TriggerPositions", DisplayName = "Trigger Positions", Type = "array", Required = true, Description = "Positions to capture" },
            new FlowParameter { Name = "CameraId", DisplayName = "Camera ID", Type = "string", Required = true, Description = "Camera identifier" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ImagesCaptured", DisplayName = "Images Captured", Type = "int" },
            new FlowParameter { Name = "CapturePositions", DisplayName = "Capture Positions", Type = "array" },
            new FlowParameter { Name = "MotionComplete", DisplayName = "Motion Complete", Type = "bool" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var targetPosition = context.GetVariable<double>("TargetPosition");
                var velocity = context.GetVariable<double>("Velocity");
                var triggerPositions = context.GetVariable<double[]>("TriggerPositions") ?? Array.Empty<double>();
                var cameraId = context.GetVariable<string>("CameraId") ?? "Cam1";

                LogSimulation($"Starting fly-capture on {axisName}: target={targetPosition}mm, velocity={velocity}mm/s");
                LogSimulation($"Trigger positions: {string.Join(", ", triggerPositions)}");

                var capturedImages = 0;
                var capturePositions = new List<double>();

                foreach (var triggerPos in triggerPositions.OrderBy(p => p))
                {
                    var travelTime = (int)(Math.Abs(triggerPos) / velocity * 1000);
                    await Task.Delay(travelTime, context.CancellationToken);

                    LogSimulation($"Triggered capture at position {triggerPos:F3}mm with camera {cameraId}");
                    capturedImages++;
                    capturePositions.Add(triggerPos);
                }

                // Complete motion
                await Task.Delay(SimulatedDelayMs, context.CancellationToken);

                context.SetVariable("ImagesCaptured", capturedImages);
                context.SetVariable("CapturePositions", capturePositions.ToArray());
                context.SetVariable("MotionComplete", true);

                LogSimulation($"Fly-capture completed: {capturedImages} images captured");

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Simulated fly-capture failed: {ex.Message}");
            }
        }

        public void LogSimulation(string activity)
        {
            Console.WriteLine($"[SIMULATION] {Name}: {activity}");
        }
    }
}
