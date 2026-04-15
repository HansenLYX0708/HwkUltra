using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.BarcodeScanner.Simulation
{
    public class SimBarcodeScannerGetLastNode : LogicNodeBase
    {
        public override string Name { get; set; } = "BarcodeScanner GetLast (Sim)";
        public override string NodeType => "BarcodeScannerGetLast";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Barcode", DisplayName = "Barcode", Type = "string" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] BarcodeScannerGetLast: Get barcode from {name}");
            await Task.Delay(10, context.CancellationToken);
            context.SetNodeOutput(Id, "Barcode", "SIM-BARCODE-12345");
            return FlowResult.Ok();
        }
    }
}
