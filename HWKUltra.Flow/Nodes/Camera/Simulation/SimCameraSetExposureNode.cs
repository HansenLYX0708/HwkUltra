using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulation
{
    /// <summary>
    /// Simulated camera set exposure node - no hardware dependency.
    /// </summary>
    public class SimCameraSetExposureNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Camera Set Exposure (Sim)";
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

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            var exposure = context.GetNodeInput<long>(Id, "ExposureTime");
            Console.WriteLine($"[SIMULATION] CameraSetExposure: {cameraName} exposure={exposure}us");
            await Task.Delay(20, context.CancellationToken);
            context.SetNodeOutput(Id, "ExposureTime", exposure);
            return FlowResult.Ok();
        }
    }
}
