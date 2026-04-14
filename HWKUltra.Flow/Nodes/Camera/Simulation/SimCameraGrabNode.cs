using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulation
{
    /// <summary>
    /// Simulated camera grab node - no hardware dependency.
    /// </summary>
    public class SimCameraGrabNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Camera Grab (Sim)";
        public override string NodeType => "CameraGrab";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "GrabStatus", DisplayName = "Grab Status", Type = "bool", Description = "Whether grab was initiated successfully" },
            new FlowParameter { Name = "Timestamp", DisplayName = "Timestamp", Type = "datetime", Description = "Grab timestamp" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] CameraGrab: Grab single frame from {cameraName}");
            await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "GrabStatus", true);
            context.SetNodeOutput(Id, "Timestamp", DateTime.Now);
            return FlowResult.Ok();
        }
    }
}
