using HWKUltra.Tray.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray reset node - reset all slot states and test state.
    /// </summary>
    public class TrayResetNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray Reset";
        public override string NodeType => "TrayReset";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public TrayResetNode(TrayRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                Service!.ResetTray(name);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Tray reset failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayReset: {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
