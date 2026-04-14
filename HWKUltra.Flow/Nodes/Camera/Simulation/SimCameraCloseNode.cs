using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulation
{
    /// <summary>
    /// Simulated camera close node - no hardware dependency.
    /// </summary>
    public class SimCameraCloseNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Camera Close (Sim)";
        public override string NodeType => "CameraClose";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether camera closed successfully" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] CameraClose: Stop grabbing on {cameraName}");
            await Task.Delay(100, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
