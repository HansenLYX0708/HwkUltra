using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.Flow.Nodes.Session
{
    /// <summary>
    /// Stamp the end time, optionally persist CSV, and mark the run Completed.
    /// The store's <c>RunCompleted</c> event fires automatically — Communication
    /// layer can subscribe there for MES upload.
    /// </summary>
    public class FinalizeTrayRunNode : DeviceNodeBase<ITestRunStore>
    {
        public override string Name { get; set; } = "Finalize Tray Run";
        public override string NodeType => "FinalizeTrayRun";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "RunKey", DisplayName = "Run Key", Type = "string", Required = false },
            new FlowParameter { Name = "CsvOutputPath", DisplayName = "CSV Output Path", Type = "string", Required = false, Description = "If set, save CSV before completing" },
            new FlowParameter { Name = "RotateCoordinates", DisplayName = "Rotate Coords", Type = "bool", Required = false }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "DefectCount", DisplayName = "Defect Count", Type = "int" },
            new FlowParameter { Name = "CsvPath", DisplayName = "CSV Path", Type = "string" }
        };

        protected override int SimulatedDelayMs => 10;

        public FinalizeTrayRunNode(ITestRunStore? store = null) : base(store) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var runKey = context.GetNodeInput<string>(Id, "RunKey") ?? "";
                if (string.IsNullOrEmpty(runKey))
                    runKey = context.GetVariable<string>("CurrentRunKey") ?? "";
                if (string.IsNullOrEmpty(runKey)) return FlowResult.Fail("RunKey is required");

                var run = Service!.Get(runKey);
                if (run is null) return FlowResult.Fail($"Run '{runKey}' not found in store");

                var csvPath = context.GetNodeInput<string>(Id, "CsvOutputPath") ?? "";
                var rotate = context.GetNodeInput<bool>(Id, "RotateCoordinates");

                int defectCount = 0;
                run.Mutate<TrayAoiReport>(report =>
                {
                    report.Session.EndTime = DateTime.Now;
                    if (!string.IsNullOrEmpty(csvPath))
                    {
                        report.CsvOutputPath = csvPath;
                        TrayAoiCsvExporter.Save(report, csvPath, rotate);
                    }
                    defectCount = report.Defects.Count;
                });

                run.Complete(TestRunStatus.Completed);

                context.SetNodeOutput(Id, "DefectCount", defectCount);
                context.SetNodeOutput(Id, "CsvPath", csvPath);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"FinalizeTrayRun failed: {ex.Message}"); }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            Console.WriteLine("[SIMULATION] FinalizeTrayRun: no store attached — noop");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "DefectCount", 0);
            context.SetNodeOutput(Id, "CsvPath", "");
            return FlowResult.Ok();
        }
    }
}
