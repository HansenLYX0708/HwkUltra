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

        protected override int SimulatedDelayMs => 50;

        public CameraTriggerNode(object? cameraService = null) : base(cameraService) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var cameraId = context.GetNodeInput<string>(Id, "CameraId") ?? "Cam1";
                var triggerMode = context.GetNodeInput<string>(Id, "TriggerMode") ?? "Software";
                var timeout = context.GetNodeInput<int>(Id, "Timeout");

                // TODO: Actual camera trigger
                // var image = await Service!.TriggerAsync(cameraId, triggerMode, timeout);
                await Task.CompletedTask;

                context.SetNodeOutput(Id, "Timestamp", DateTime.Now);
                context.SetNodeOutput(Id, "TriggerStatus", true);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera trigger failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var cameraId = context.GetNodeInput<string>(Id, "CameraId") ?? "Cam1";
            Console.WriteLine($"[SIMULATION] CameraTrigger: Trigger camera {cameraId}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Timestamp", DateTime.Now);
            context.SetNodeOutput(Id, "TriggerStatus", true);
            return FlowResult.Ok();
        }
    }
}
