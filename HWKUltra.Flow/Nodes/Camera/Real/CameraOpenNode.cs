using HWKUltra.Camera.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Real
{
    /// <summary>
    /// Camera open node - opens and initializes the camera controller.
    /// Typically placed at the beginning of a flow to initialize all cameras.
    /// </summary>
    public class CameraOpenNode : DeviceNodeBase<CameraRouter>
    {
        public override string Name { get; set; } = "Camera Open";
        public override string NodeType => "CameraOpen";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CameraName", DisplayName = "Camera Name", Type = "string", Required = true, Description = "Logical camera name to verify availability" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Status", DisplayName = "Status", Type = "bool", Description = "Whether camera opened successfully" }
        };

        protected override int SimulatedDelayMs => 200;

        public CameraOpenNode(CameraRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "";
                if (string.IsNullOrEmpty(cameraName))
                    return FlowResult.Fail("CameraName is required");

                if (!Service!.HasCamera(cameraName))
                    return FlowResult.Fail($"Camera '{cameraName}' not found in configuration");

                context.SetNodeOutput(Id, "Status", true);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera open failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] CameraOpen: Verify camera {cameraName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Status", true);
            return FlowResult.Ok();
        }
    }
}
