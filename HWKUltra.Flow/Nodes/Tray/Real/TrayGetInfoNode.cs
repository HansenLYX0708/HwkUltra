using HWKUltra.Tray.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray get info node - get tray statistical information.
    /// </summary>
    public class TrayGetInfoNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray GetInfo";
        public override string NodeType => "TrayGetInfo";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "TotalSlots", DisplayName = "Total Slots", Type = "int" },
            new FlowParameter { Name = "TestedCount", DisplayName = "Tested", Type = "int" },
            new FlowParameter { Name = "PassCount", DisplayName = "Pass", Type = "int" },
            new FlowParameter { Name = "FailCount", DisplayName = "Fail", Type = "int" },
            new FlowParameter { Name = "ErrorCount", DisplayName = "Error", Type = "int" },
            new FlowParameter { Name = "TestState", DisplayName = "TestState", Type = "int" }
        };

        public TrayGetInfoNode(TrayRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                var info = Service!.GetTrayInfo(name);
                context.SetNodeOutput(Id, "TotalSlots", info.TotalSlots);
                context.SetNodeOutput(Id, "TestedCount", info.TestedCount);
                context.SetNodeOutput(Id, "PassCount", info.PassCount);
                context.SetNodeOutput(Id, "FailCount", info.FailCount);
                context.SetNodeOutput(Id, "ErrorCount", info.ErrorCount);
                context.SetNodeOutput(Id, "TestState", (int)info.TestState);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Tray get info failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayGetInfo: {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "TotalSlots", 240);
            context.SetNodeOutput(Id, "TestedCount", 0);
            context.SetNodeOutput(Id, "PassCount", 0);
            context.SetNodeOutput(Id, "FailCount", 0);
            context.SetNodeOutput(Id, "ErrorCount", 0);
            context.SetNodeOutput(Id, "TestState", 0);
            return FlowResult.Ok();
        }
    }
}
