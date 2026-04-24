using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Read a PLC bit by configured bit name.
    /// </summary>
    public class PlcReadBitNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "PLC Read Bit";
        public override string NodeType => "PlcReadBit";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitName", DisplayName = "Bit Name", Type = "string", Required = true, Description = "Named bit from PLC BitMap (e.g. LeftTrayState)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool" }
        };

        public PlcReadBitNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "BitName") ?? "";
                if (string.IsNullOrEmpty(name)) return FlowResult.Fail("BitName is required");

                var plc = Service!.Plc;
                if (plc == null) return FlowResult.Fail("Controller is not an IGenericPlcController");

                var value = plc.ReadBit(name);
                context.SetNodeOutput(Id, "Value", value);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"PlcReadBit failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "BitName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] PlcReadBit: {name} = false");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Value", false);
            return FlowResult.Ok();
        }
    }
}
