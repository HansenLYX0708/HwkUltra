using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Send a named PLC command (pre-configured via CommandMap). Blocks until the configured
    /// success feedback is observed, failure is observed, or timeout elapses.
    /// </summary>
    public class PlcSendCommandNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "PLC Send Command";
        public override string NodeType => "PlcSendCommand";
        protected override int SimulatedDelayMs => 200;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CommandName", DisplayName = "Command Name", Type = "string", Required = true, Description = "Command key from PLC CommandMap (e.g. RobotPickTray)" },
            new FlowParameter { Name = "TimeoutMs",   DisplayName = "Timeout (ms)", Type = "int",    Required = false, DefaultValue = 0, Description = "Override timeout; 0 = use config default" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool" },
            new FlowParameter { Name = "Message", DisplayName = "Message", Type = "string" }
        };

        public PlcSendCommandNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "CommandName") ?? "";
                var timeout = context.GetNodeInput<int>(Id, "TimeoutMs");

                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("CommandName is required");

                var plc = Service!.Plc;
                if (plc == null)
                    return FlowResult.Fail("Underlying controller is not an IGenericPlcController");

                var ok = plc.SendCommand(name, timeout, out var message);
                context.SetNodeOutput(Id, "Success", ok);
                context.SetNodeOutput(Id, "Message", message);
                return ok ? FlowResult.Ok() : FlowResult.Fail(message);
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"PlcSendCommand failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "CommandName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] PlcSendCommand: {name} -> Success");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Success", true);
            context.SetNodeOutput(Id, "Message", $"Simulated command '{name}' completed");
            return FlowResult.Ok();
        }
    }
}
