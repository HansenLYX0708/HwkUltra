using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Vision.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Run DL inference via an <see cref="IInferenceEngine"/> service.
    /// When the service is null the node runs in simulated mode (empty result).
    /// </summary>
    public class InferenceNode : DeviceNodeBase<IInferenceEngine>
    {
        public override string Name { get; set; } = "DL Inference";
        public override string NodeType => "DLInference";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitmapVar", DisplayName = "Bitmap Variable", Type = "string", Required = true, Description = "Context variable holding the Bitmap to infer" }
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
                var varName = context.GetNodeInput<string>(Id, "BitmapVar") ?? "";
                if (string.IsNullOrEmpty(varName)) return FlowResult.Fail("BitmapVar is required");
                var bmp = context.GetVariable<Bitmap>(varName);
                if (bmp is null) return FlowResult.Fail($"Variable '{varName}' not found or not a Bitmap");
                var result = Service!.Predict(bmp);
                context.SetNodeOutput(Id, "RawResult", result);
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
            return FlowResult.Ok();
        }
    }
}
