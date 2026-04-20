using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.TestRun.Core;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.Flow.Nodes.Session
{
    /// <summary>
    /// Create a new <see cref="TrayAoiReport"/>, register it in the shared
    /// <see cref="ITestRunStore"/>, and store the run key into FlowContext so
    /// downstream nodes can look it up.
    /// </summary>
    public class StartTrayRunNode : DeviceNodeBase<ITestRunStore>
    {
        public override string Name { get; set; } = "Start Tray Run";
        public override string NodeType => "StartTrayRun";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "RunKey", DisplayName = "Run Key", Type = "string", Required = true, Description = "Unique key (e.g., tray instance name)" },
            new FlowParameter { Name = "TrayName", DisplayName = "Tray Name", Type = "string", Required = true },
            new FlowParameter { Name = "Rows", DisplayName = "Rows", Type = "int", Required = true },
            new FlowParameter { Name = "Cols", DisplayName = "Cols", Type = "int", Required = true },
            new FlowParameter { Name = "LotId", DisplayName = "Lot ID", Type = "string", Required = false },
            new FlowParameter { Name = "OperatorId", DisplayName = "Operator ID", Type = "string", Required = false },
            new FlowParameter { Name = "ProductName", DisplayName = "Product Name", Type = "string", Required = false },
            new FlowParameter { Name = "SerialNumber", DisplayName = "Tray Serial", Type = "string", Required = false }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "RunKey", DisplayName = "Run Key", Type = "string" }
        };

        protected override int SimulatedDelayMs => 10;

        public StartTrayRunNode(ITestRunStore? store = null) : base(store) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var runKey = context.GetNodeInput<string>(Id, "RunKey") ?? "";
                var trayName = context.GetNodeInput<string>(Id, "TrayName") ?? "";
                var rows = context.GetNodeInput<int>(Id, "Rows");
                var cols = context.GetNodeInput<int>(Id, "Cols");
                if (string.IsNullOrEmpty(runKey)) return FlowResult.Fail("RunKey is required");
                if (rows <= 0 || cols <= 0) return FlowResult.Fail("Rows and Cols must be positive");

                var report = new TrayAoiReport(rows, cols)
                {
                    TrayName = trayName
                };
                report.Session.LotId = context.GetNodeInput<string>(Id, "LotId") ?? "";
                report.Session.OperatorId = context.GetNodeInput<string>(Id, "OperatorId") ?? "";
                report.Session.ProductName = context.GetNodeInput<string>(Id, "ProductName") ?? "";
                report.Session.SerialNumber = context.GetNodeInput<string>(Id, "SerialNumber") ?? "";
                report.Session.StartTime = DateTime.Now;

                Service!.Start(runKey, report);
                context.SetVariable("CurrentRunKey", runKey);
                context.SetNodeOutput(Id, "RunKey", runKey);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"StartTrayRun failed: {ex.Message}"); }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var runKey = context.GetNodeInput<string>(Id, "RunKey") ?? "SimRun";
            Console.WriteLine($"[SIMULATION] StartTrayRun: {runKey} (no store attached)");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetVariable("CurrentRunKey", runKey);
            context.SetNodeOutput(Id, "RunKey", runKey);
            return FlowResult.Ok();
        }
    }
}
