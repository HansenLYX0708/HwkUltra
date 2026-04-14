using HWKUltra.Tray.Core;
using HWKUltra.Tray.Abstractions;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray set slot state node - set the state of a specific slot.
    /// </summary>
    public class TraySetSlotStateNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray SetSlotState";
        public override string NodeType => "TraySetSlotState";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Row", DisplayName = "Row", Type = "int", Required = true },
            new FlowParameter { Name = "Col", DisplayName = "Col", Type = "int", Required = true },
            new FlowParameter { Name = "State", DisplayName = "State", Type = "int", Required = true, Description = "0=Empty,1=Present,2=Pass,3=Fail,4=Error,5=Unknown" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public TraySetSlotStateNode(TrayRouter? router = null) : base(router) { }

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
                int stateVal = context.GetNodeInput<int>(Id, "State");
                var state = (SlotState)stateVal;
                Service!.SetSlotState(name, row, col, state);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Tray set slot state failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TraySetSlotState: {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
