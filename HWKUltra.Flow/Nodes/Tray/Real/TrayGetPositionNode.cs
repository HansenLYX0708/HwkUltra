using HWKUltra.Core;
using HWKUltra.Tray.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray get position node - get pocket 3D position at (row, col).
    /// </summary>
    public class TrayGetPositionNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray GetPosition";
        public override string NodeType => "TrayGetPosition";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "Row", DisplayName = "Row", Type = "int", Required = true, Description = "0-based row index" },
            new FlowParameter { Name = "Col", DisplayName = "Col", Type = "int", Required = true, Description = "0-based col index" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "X", DisplayName = "X", Type = "double" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "double" },
            new FlowParameter { Name = "Z", DisplayName = "Z", Type = "double" },
            new FlowParameter { Name = "AxisPositionJson", DisplayName = "AxisPosition JSON", Type = "string", Description = "Position as AxisPosition JSON for motion nodes" }
        };

        public TrayGetPositionNode(TrayRouter? router = null) : base(router) { }

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
                var pos = Service!.GetPocketPosition(name, row, col);
                var (x, y, z) = pos.ToXYZ();
                context.SetNodeOutput(Id, "X", x);
                context.SetNodeOutput(Id, "Y", y);
                context.SetNodeOutput(Id, "Z", z);
                context.SetNodeOutput(Id, "AxisPositionJson", pos.ToJson());
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Tray get position failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayGetPosition: {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "X", 100.0);
            context.SetNodeOutput(Id, "Y", 200.0);
            context.SetNodeOutput(Id, "Z", 0.0);
            context.SetNodeOutput(Id, "AxisPositionJson", Pos.XYZ(100.0, 200.0, 0.0).ToJson());
            return FlowResult.Ok();
        }
    }
}
