using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulation
{
    /// <summary>
    /// Simulated camera set gain node - no hardware dependency.
    /// </summary>
    public class SimCameraSetGainNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Camera Set Gain (Sim)";
        public override string NodeType => "CameraSetGain";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name" },
            new FlowParameter { Name = "Gain", DisplayName = "Gain", Type = "long", Required = true, DefaultValue = 0L, Description = "Gain value" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Gain", DisplayName = "Gain", Type = "long", Description = "Actual gain value set" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            var gain = context.GetNodeInput<long>(Id, "Gain");
            Console.WriteLine($"[SIMULATION] CameraSetGain: {cameraName} gain={gain}");
            await Task.Delay(20, context.CancellationToken);
            context.SetNodeOutput(Id, "Gain", gain);
            return FlowResult.Ok();
        }
    }
}
