using HWKUltra.Camera.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Real
{
    /// <summary>
    /// Camera set exposure node - sets exposure time for the specified camera.
    /// </summary>
    public class CameraSetExposureNode : DeviceNodeBase<CameraRouter>
    {
        public override string Name { get; set; } = "Camera Set Exposure";
        public override string NodeType => "CameraSetExposure";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name" },
            new FlowParameter { Name = "ExposureTime", DisplayName = "Exposure Time (us)", Type = "long", Required = true, DefaultValue = 50L, Description = "Exposure time in microseconds" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ExposureTime", DisplayName = "Exposure Time", Type = "long", Description = "Actual exposure time set" }
        };

        protected override int SimulatedDelayMs => 20;

        public CameraSetExposureNode(CameraRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "";
                if (string.IsNullOrEmpty(cameraName))
                    return FlowResult.Fail("CameraName is required");

                var exposure = context.GetNodeInput<long>(Id, "ExposureTime");

                Service!.SetExposureTime(cameraName, exposure);
                var actual = Service.GetExposureTime(cameraName);

                context.SetNodeOutput(Id, "ExposureTime", actual);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera set exposure failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            var exposure = context.GetNodeInput<long>(Id, "ExposureTime");
            Console.WriteLine($"[SIMULATION] CameraSetExposure: {cameraName} exposure={exposure}us");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ExposureTime", exposure);
            return FlowResult.Ok();
        }
    }
}
