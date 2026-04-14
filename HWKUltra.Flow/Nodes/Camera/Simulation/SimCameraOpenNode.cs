using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulation
{
    /// <summary>
    /// Simulated camera open node - no hardware dependency.
    /// </summary>
    public class SimCameraOpenNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Camera Open (Sim)";
        public override string NodeType => "CameraOpen";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name to verify availability" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether camera opened successfully" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] CameraOpen: Verify camera {cameraName}");
            await Task.Delay(200, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
