using HWKUltra.Tray.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray init node - set tray shape (rows x cols).
    /// </summary>
    public class TrayInitNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray Init";
        public override string NodeType => "TrayInit";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Tray instance name" },
            new FlowParameter { Name = "Rows", DisplayName = "Rows", Type = "int", Required = true, Description = "Number of rows" },
            new FlowParameter { Name = "Cols", DisplayName = "Cols", Type = "int", Required = true, Description = "Number of columns" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public TrayInitNode(TrayRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                int rows = context.GetNodeInput<int>(Id, "Rows");
                int cols = context.GetNodeInput<int>(Id, "Cols");
                Service!.SetShape(name, rows, cols);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Tray init failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayInit: {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
