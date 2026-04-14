using HWKUltra.Camera.Abstractions;
using HWKUltra.Camera.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Real
{
    /// <summary>
    /// Camera set trigger mode node - sets trigger mode (Freerun / ExternalHardware / Software).
    /// </summary>
    public class CameraSetTriggerModeNode : DeviceNodeBase<CameraRouter>
    {
        public override string Name { get; set; } = "Camera Set Trigger Mode";
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

        protected override int SimulatedDelayMs => 30;

        public CameraSetTriggerModeNode(CameraRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "";
                if (string.IsNullOrEmpty(cameraName))
                    return FlowResult.Fail("CameraName is required");

                var modeStr = context.GetNodeInput<string>(Id, "TriggerMode") ?? "Freerun";
                if (!Enum.TryParse<CameraTriggerMode>(modeStr, true, out var mode))
                    return FlowResult.Fail($"Invalid trigger mode: {modeStr}. Valid: Freerun, ExternalHardware, Software");

                Service!.SetTriggerMode(cameraName, mode);

                context.SetNodeOutput(Id, "TriggerMode", mode.ToString());
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera set trigger mode failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            var modeStr = context.GetNodeInput<string>(Id, "TriggerMode") ?? "Freerun";
            Console.WriteLine($"[SIMULATION] CameraSetTriggerMode: {cameraName} mode={modeStr}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "TriggerMode", modeStr);
            return FlowResult.Ok();
        }
    }
}
