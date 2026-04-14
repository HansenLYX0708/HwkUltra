using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulation
{
    /// <summary>
    /// Simulated camera trigger node - no hardware dependency.
    /// </summary>
    public class SimCameraTriggerNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Camera Trigger (Sim)";
        public override string NodeType => "CameraTrigger";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name (e.g., DetectCam)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Timestamp", DisplayName = "Timestamp", Type = "datetime", Description = "Trigger timestamp" },
            new FlowParameter { Name = "TriggerStatus", DisplayName = "Trigger Status", Type = "bool", Description = "Whether trigger was successful" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] CameraTrigger: Send software trigger to {cameraName}");
            await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "Timestamp", DateTime.Now);
            context.SetNodeOutput(Id, "TriggerStatus", true);
            return FlowResult.Ok();
        }
    }
}
