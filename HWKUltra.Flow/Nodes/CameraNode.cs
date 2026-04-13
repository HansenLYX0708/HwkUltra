using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Nodes
{
    /// <summary>
    /// Camera capture node
    /// </summary>
    public class CameraNode : IFlowNode
    {
        // TODO: Inject ICameraService
        // private readonly ICameraService _cameraService;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Camera Capture";
        public string NodeType => "Camera";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraId", DisplayName = "Camera ID", Type = "string", Required = true, Description = "Camera identifier" },
            new FlowParameter { Name = "ExposureTime", DisplayName = "Exposure Time", Type = "double", Required = false, DefaultValue = 10000.0, Description = "Microseconds" },
            new FlowParameter { Name = "Gain", DisplayName = "Gain", Type = "double", Required = false, DefaultValue = 1.0 },
            new FlowParameter { Name = "SavePath", DisplayName = "Save Path", Type = "string", Required = false, Description = "Image save path" },
            new FlowParameter { Name = "TriggerMode", DisplayName = "Trigger Mode", Type = "string", Required = false, DefaultValue = "Software", Description = "Software/Hardware" }
        };

        public List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ImageData", DisplayName = "Image Data", Type = "image", Description = "Captured image" },
            new FlowParameter { Name = "ImagePath", DisplayName = "Image Path", Type = "string", Description = "Saved image path" },
            new FlowParameter { Name = "Timestamp", DisplayName = "Timestamp", Type = "datetime", Description = "Capture time" }
        };

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var cameraId = context.GetVariable<string>("CameraId") ?? "Cam1";
                var exposureTime = context.GetVariable<double>("ExposureTime");
                var gain = context.GetVariable<double>("Gain");
                var savePath = context.GetVariable<string>("SavePath");
                var triggerMode = context.GetVariable<string>("TriggerMode") ?? "Software";

                // TODO: Actually call camera service
                // var image = await _cameraService.CaptureAsync(cameraId, exposureTime, gain);

                // Simulate capture
                await Task.Delay(100);

                // Set outputs
                context.SetVariable("ImagePath", $"{savePath}/{cameraId}_{DateTime.Now:yyyyMMddHHmmss}.bmp");
                context.SetVariable("Timestamp", DateTime.Now);

                Console.WriteLine($"[Camera] Camera {cameraId} capture completed");

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera capture failed: {ex.Message}");
            }
        }
    }
}
