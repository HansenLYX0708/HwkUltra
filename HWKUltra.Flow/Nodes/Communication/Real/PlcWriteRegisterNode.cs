using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Write a 32-bit PLC register by configured register name.
    /// </summary>
    public class PlcWriteRegisterNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "PLC Write Register";
        public override string NodeType => "PlcWriteRegister";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "RegisterName", DisplayName = "Register Name", Type = "string", Required = true },
            new FlowParameter { Name = "Value",        DisplayName = "Value",         Type = "int",    Required = true, DefaultValue = 0 }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public PlcWriteRegisterNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "RegisterName") ?? "";
                var value = context.GetNodeInput<int>(Id, "Value");

                if (string.IsNullOrEmpty(name)) return FlowResult.Fail("RegisterName is required");
                var plc = Service!.Plc;
                if (plc == null) return FlowResult.Fail("Controller is not an IGenericPlcController");

                plc.WriteRegister(name, value);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"PlcWriteRegister failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "RegisterName") ?? "Unknown";
            var value = context.GetNodeInput<int>(Id, "Value");
            Console.WriteLine($"[SIMULATION] PlcWriteRegister: {name} = {value}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
