using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Write a PLC bit by configured bit name.
    /// </summary>
    public class PlcWriteBitNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "PLC Write Bit";
        public override string NodeType => "PlcWriteBit";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitName", DisplayName = "Bit Name", Type = "string", Required = true },
            new FlowParameter { Name = "Value",   DisplayName = "Value",    Type = "bool",   Required = true, DefaultValue = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public PlcWriteBitNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "BitName") ?? "";
                var valueStr = context.GetNodeInput<string>(Id, "Value") ?? "true";
                var value = valueStr != "false";

                if (string.IsNullOrEmpty(name)) return FlowResult.Fail("BitName is required");
                var plc = Service!.Plc;
                if (plc == null) return FlowResult.Fail("Controller is not an IGenericPlcController");

                plc.WriteBit(name, value);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"PlcWriteBit failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "BitName") ?? "Unknown";
            var value = context.GetNodeInput<string>(Id, "Value") ?? "true";
            Console.WriteLine($"[SIMULATION] PlcWriteBit: {name} = {value}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
