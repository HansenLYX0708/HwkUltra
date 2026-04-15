using HWKUltra.BarcodeScanner.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.BarcodeScanner.Real
{
    /// <summary>
    /// Open a barcode scanner serial port.
    /// </summary>
    public class BarcodeScannerOpenNode : DeviceNodeBase<BarcodeScannerRouter>
    {
        public override string Name { get; set; } = "BarcodeScanner Open";
        public override string NodeType => "BarcodeScannerOpen";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Scanner instance name" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public BarcodeScannerOpenNode(BarcodeScannerRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                Service!.Open(name);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"BarcodeScanner open failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] BarcodeScannerOpen: Open {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
