using HWKUltra.Camera.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Camera.Real
{
    /// <summary>
    /// Camera grab node - grabs a single frame from the specified camera.
    /// </summary>
    public class CameraGrabNode : DeviceNodeBase<CameraRouter>
    {
        public override string Name { get; set; } = "Camera Grab";
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

        protected override int SimulatedDelayMs => 50;

        public CameraGrabNode(CameraRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "";
                if (string.IsNullOrEmpty(cameraName))
                    return FlowResult.Fail("CameraName is required");

                bool result = Service!.GrabOne(cameraName);

                context.SetNodeOutput(Id, "GrabStatus", result);
                context.SetNodeOutput(Id, "Timestamp", DateTime.Now);

                return result ? FlowResult.Ok() : FlowResult.Fail("Camera is already grabbing");
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Camera grab failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var cameraName = context.GetNodeInput<string>(Id, "CameraName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] CameraGrab: Grab single frame from {cameraName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "GrabStatus", true);
            context.SetNodeOutput(Id, "Timestamp", DateTime.Now);
            return FlowResult.Ok();
        }
    }
}
