using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Utils;
using HWKUltra.Vision.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Run DL inference via an <see cref="IInferenceEngine"/> service.
    /// Accepts image as absolute path or context variable (Bitmap/Mat/byte[]/float[]/path).
    /// When the service is null the node runs in simulated mode (empty result).
    /// </summary>
    public class InferenceNode : DeviceNodeBase<IInferenceEngine>
    {
        public override string Name { get; set; } = "DL Inference";
        public override string NodeType => "DLInference";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Image", DisplayName = "Image", Type = "string", Required = true, Description = "Absolute path OR context variable" },
            new FlowParameter { Name = "Width", DisplayName = "Width", Type = "int", Required = false },
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "int", Required = false },
            new FlowParameter { Name = "Channels", DisplayName = "Channels", Type = "int", Required = false, DefaultValue = 1 },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable", Type = "string", Required = false, Description = "Shared-context name for the RawResult int[]" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "RawResult", DisplayName = "Raw Int Result", Type = "int[]" }
        };

        protected override int SimulatedDelayMs => 10;

        public InferenceNode(IInferenceEngine? engine = null) : base(engine) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var image = context.GetNodeInput<string>(Id, "Image") ?? "";
                if (string.IsNullOrEmpty(image)) return FlowResult.Fail("Image is required");
                int w = context.GetNodeInput<int>(Id, "Width");
                int h = context.GetNodeInput<int>(Id, "Height");
                int ch = context.GetNodeInput<int>(Id, "Channels"); if (ch == 0) ch = 1;
                using var resolved = ImageInputResolver.ResolveBitmap(context, image, w, h, ch);
                var result = Service!.Predict(resolved.Bitmap);
                context.SetNodeOutput(Id, "RawResult", result);
                VisionOutput.Publish(context, Id, "OutputVariable", result);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"Inference failed: {ex.Message}"); }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            Console.WriteLine($"[SIMULATION] DLInference: no backend — returning empty result");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            int[] sim = new int[256];
            for (int i = 0; i < 6; i++) sim[i] = -1;
            context.SetNodeOutput(Id, "RawResult", sim);
            VisionOutput.Publish(context, Id, "OutputVariable", sim);
            return FlowResult.Ok();
        }
    }
}
