using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Read a 32-bit PLC register by configured register name.
    /// </summary>
    public class PlcReadRegisterNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "PLC Read Register";
        public override string NodeType => "PlcReadRegister";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "RegisterName", DisplayName = "Register Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "int" }
        };

        public PlcReadRegisterNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "RegisterName") ?? "";
                if (string.IsNullOrEmpty(name)) return FlowResult.Fail("RegisterName is required");
                var plc = Service!.Plc;
                if (plc == null) return FlowResult.Fail("Controller is not an IGenericPlcController");

                var v = plc.ReadRegister(name);
                context.SetNodeOutput(Id, "Value", v);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"PlcReadRegister failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "RegisterName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] PlcReadRegister: {name} = 0");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Value", 0);
            return FlowResult.Ok();
        }
    }
}
