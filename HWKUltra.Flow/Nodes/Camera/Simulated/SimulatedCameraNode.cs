using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulated
{
    /// <summary>
    /// Simulated camera node - for testing without hardware
    /// </summary>
    public class SimulatedCameraNode : LogicNodeBase, ISimulatedNode
    {
        public override string Name { get; set; } = "Simulated Camera";
        public override string NodeType => "Camera";

        public bool SimulateExecution { get; set; } = true;
        public int SimulatedDelayMs { get; set; } = 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraId", DisplayName = "Camera ID", Type = "string", Required = true, Description = "Camera identifier" },
            new FlowParameter { Name = "ExposureTime", DisplayName = "Exposure Time", Type = "double", Required = false, DefaultValue = 10000.0, Description = "Microseconds" },
            new FlowParameter { Name = "Gain", DisplayName = "Gain", Type = "double", Required = false, DefaultValue = 1.0 },
            new FlowParameter { Name = "SavePath", DisplayName = "Save Path", Type = "string", Required = false, Description = "Image save path" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ImageData", DisplayName = "Image Data", Type = "image", Description = "Simulated image reference" },
            new FlowParameter { Name = "ImagePath", DisplayName = "Image Path", Type = "string", Description = "Saved image path" },
            new FlowParameter { Name = "Timestamp", DisplayName = "Timestamp", Type = "datetime", Description = "Capture timestamp" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var cameraId = context.GetVariable<string>("CameraId") ?? "Cam1";
                var exposureTime = context.GetVariable<double>("ExposureTime");
                var gain = context.GetVariable<double>("Gain");
                var savePath = context.GetVariable<string>("SavePath");

                LogSimulation($"Capturing from camera {cameraId}, exposure={exposureTime}us, gain={gain:F2}");
                await Task.Delay(SimulatedDelayMs, context.CancellationToken);

                var timestamp = DateTime.Now;
                var imagePath = $"{savePath}/{cameraId}_{timestamp:yyyyMMddHHmmss}.bmp";

                context.SetVariable("ImagePath", imagePath);
                context.SetVariable("Timestamp", timestamp);
                context.SetVariable("ImageData", $"[SIMULATED_IMAGE_{cameraId}]");

                LogSimulation($"Capture completed: {imagePath}");

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Simulated camera capture failed: {ex.Message}");
            }
        }

        public void LogSimulation(string activity)
        {
            Console.WriteLine($"[SIMULATION] {Name}: {activity}");
        }
    }
}
