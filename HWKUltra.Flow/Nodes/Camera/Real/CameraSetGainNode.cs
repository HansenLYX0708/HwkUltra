using HWKUltra.Camera.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Real
{
    /// <summary>
    /// Camera set gain node - sets gain value for the specified camera.
    /// </summary>
    public class CameraSetGainNode : DeviceNodeBase<CameraRouter>
    {
        public override string Name { get; set; } = "Camera Set Gain";
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

        protected override int SimulatedDelayMs => 20;

        public CameraSetGainNode(CameraRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "";
                if (string.IsNullOrEmpty(cameraName))
                    return FlowResult.Fail("CameraName is required");

                var gain = context.GetNodeInput<long>(Id, "Gain");

                Service!.SetGain(cameraName, gain);
                var actual = Service.GetGain(cameraName);

                context.SetNodeOutput(Id, "Gain", actual);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera set gain failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            var gain = context.GetNodeInput<long>(Id, "Gain");
            Console.WriteLine($"[SIMULATION] CameraSetGain: {cameraName} gain={gain}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Gain", gain);
            return FlowResult.Ok();
        }
    }
}
