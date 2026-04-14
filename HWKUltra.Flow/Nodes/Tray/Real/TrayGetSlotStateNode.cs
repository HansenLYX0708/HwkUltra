using HWKUltra.Tray.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray get slot state node - get the state of a specific slot.
    /// </summary>
    public class TrayGetSlotStateNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray GetSlotState";
        public override string NodeType => "TrayGetSlotState";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Row", DisplayName = "Row", Type = "int", Required = true },
            new FlowParameter { Name = "Col", DisplayName = "Col", Type = "int", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "State", DisplayName = "State", Type = "int", Description = "0=Empty,1=Present,2=Pass,3=Fail,4=Error,5=Unknown" }
        };

        public TrayGetSlotStateNode(TrayRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                int row = context.GetNodeInput<int>(Id, "Row");
                int col = context.GetNodeInput<int>(Id, "Col");
                var state = Service!.GetSlotState(name, row, col);
                context.SetNodeOutput(Id, "State", (int)state);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Tray get slot state failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayGetSlotState: {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "State", 0);
            return FlowResult.Ok();
        }
    }
}
