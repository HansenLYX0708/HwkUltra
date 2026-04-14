using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Simulation
{
    /// <summary>
    /// Simulated camera set trigger mode node - no hardware dependency.
    /// </summary>
    public class SimCameraSetTriggerModeNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Camera Set Trigger Mode (Sim)";
        public override string NodeType => "CameraSetTriggerMode";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name" },
            new FlowParameter { Name = "TriggerMode", DisplayName = "Trigger Mode", Type = "string", Required = true, DefaultValue = "Freerun", Description = "Freerun, ExternalHardware, or Software" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "TriggerMode", DisplayName = "Trigger Mode", Type = "string", Description = "Trigger mode that was set" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            var modeStr = context.GetNodeInput<string>(Id, "TriggerMode") ?? "Freerun";
            Console.WriteLine($"[SIMULATION] CameraSetTriggerMode: {cameraName} mode={modeStr}");
            await Task.Delay(30, context.CancellationToken);
            context.SetNodeOutput(Id, "TriggerMode", modeStr);
            return FlowResult.Ok();
        }
    }
}
