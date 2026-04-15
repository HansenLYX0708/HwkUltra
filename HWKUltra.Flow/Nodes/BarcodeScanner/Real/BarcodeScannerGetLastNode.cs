using HWKUltra.BarcodeScanner.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.BarcodeScanner.Real
{
    /// <summary>
    /// Get the last received barcode from a scanner instance.
    /// </summary>
    public class BarcodeScannerGetLastNode : DeviceNodeBase<BarcodeScannerRouter>
    {
        public override string Name { get; set; } = "BarcodeScanner GetLast";
        public override string NodeType => "BarcodeScannerGetLast";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Scanner instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Barcode", DisplayName = "Barcode", Type = "string", Description = "Last received barcode" }
        };

        public BarcodeScannerGetLastNode(BarcodeScannerRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                var barcode = Service!.GetLastBarcode(name) ?? "";
                context.SetNodeOutput(Id, "Barcode", barcode);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"BarcodeScanner get last failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] BarcodeScannerGetLast: Get barcode from {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Barcode", "SIM-BARCODE-12345");
            return FlowResult.Ok();
        }
    }
}
