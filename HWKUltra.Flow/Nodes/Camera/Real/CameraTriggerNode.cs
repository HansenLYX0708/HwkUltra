using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Real
{
    /// <summary>
    /// Camera trigger node - triggers camera capture
    /// </summary>
    public class CameraTriggerNode : DeviceNodeBase<object>  // TODO: Replace object with ICameraService
    {
        public override string Name { get; set; } = "Camera Trigger";
        public override string NodeType => "CameraTrigger";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraId", DisplayName = "Camera ID", Type = "string", Required = true, Description = "Camera identifier" },
            new FlowParameter { Name = "TriggerMode", DisplayName = "Trigger Mode", Type = "string", Required = false, DefaultValue = "Software", Description = "Software, Hardware, Encoder" },
            new FlowParameter { Name = "Timeout", DisplayName = "Timeout", Type = "int", Required = false, DefaultValue = 5000, Description = "Timeout in ms" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ImageData", DisplayName = "Image Data", Type = "image", Description = "Captured image" },
            new FlowParameter { Name = "Timestamp", DisplayName = "Timestamp", Type = "datetime", Description = "Capture timestamp" },
            new FlowParameter { Name = "TriggerStatus", DisplayName = "Trigger Status", Type = "bool", Description = "Whether trigger was successful" }
        };

        public CameraTriggerNode(object? cameraService = null) : base(cameraService) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var cameraId = context.GetVariable<string>("CameraId") ?? "Cam1";
                var triggerMode = context.GetVariable<string>("TriggerMode") ?? "Software";
                var timeout = context.GetVariable<int>("Timeout");

                if (IsSimulated)
                {
                    Console.WriteLine($"[CameraTrigger] Simulating trigger for camera {cameraId}, mode={triggerMode}");
                    await Task.Delay(50, context.CancellationToken);

                    context.SetVariable("Timestamp", DateTime.Now);
                    context.SetVariable("TriggerStatus", true);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                // TODO: Actual camera trigger
                // var image = await Service!.TriggerAsync(cameraId, triggerMode, timeout);

                context.SetVariable("Timestamp", DateTime.Now);
                context.SetVariable("TriggerStatus", true);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera trigger failed: {ex.Message}");
            }
        }
    }
}
