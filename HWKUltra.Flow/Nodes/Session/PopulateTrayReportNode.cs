using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Flow.Nodes.Vision;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.TestRun.Reports;
using HWKUltra.Vision.Abstractions;

namespace HWKUltra.Flow.Nodes.Session
{
    /// <summary>
    /// Enrich a list of raw <see cref="VisionDetection"/>s with slot context
    /// and append into the active run's <see cref="TrayAoiReport"/> (thread-safe
    /// via <see cref="ITestRun.Mutate{TReport}"/>).
    /// </summary>
    public class PopulateTrayReportNode : DeviceNodeBase<ITestRunStore>
    {
        public override string Name { get; set; } = "Populate Tray Report";
        public override string NodeType => "PopulateTrayReport";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "RunKey", DisplayName = "Run Key", Type = "string", Required = true },
            new FlowParameter { Name = "DetectionsVar", DisplayName = "Detections Variable", Type = "string", Required = true, Description = "Context variable holding List<VisionDetection>" },
            new FlowParameter { Name = "Row", DisplayName = "Row (1-based)", Type = "int", Required = true },
            new FlowParameter { Name = "Col", DisplayName = "Col (1-based)", Type = "int", Required = true },
            new FlowParameter { Name = "ImgRows", DisplayName = "Image Rows", Type = "int", Required = false },
            new FlowParameter { Name = "ImgCols", DisplayName = "Image Cols", Type = "int", Required = false },
            new FlowParameter { Name = "SlotDefectCode", DisplayName = "Slot Defect Code", Type = "string", Required = false, Description = "Set SlotDefectCodes[row,col] to this value if non-empty (e.g. 'Pass' or 'A2')" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "AppendedCount", DisplayName = "Appended Count", Type = "int" }
        };

        protected override int SimulatedDelayMs => 5;

        public PopulateTrayReportNode(ITestRunStore? store = null) : base(store) { }

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

                var detectionsVar = context.GetNodeInput<string>(Id, "DetectionsVar") ?? "";
                var detections = context.GetVariable<List<VisionDetection>>(detectionsVar);
                if (detections is null) return FlowResult.Fail($"Variable '{detectionsVar}' is null or not a List<VisionDetection>");

                var row = context.GetNodeInput<int>(Id, "Row");
                var col = context.GetNodeInput<int>(Id, "Col");
                var imgRows = context.GetNodeInput<int>(Id, "ImgRows");
                var imgCols = context.GetNodeInput<int>(Id, "ImgCols");
                var slotCode = context.GetNodeInput<string>(Id, "SlotDefectCode") ?? "";

                int appended = 0;
                run.Mutate<TrayAoiReport>(report =>
                {
                    foreach (var d in detections)
                    {
                        report.Defects.Add(VisionDefectMapper.ToDefectDetail(d, row, col, imgRows, imgCols));
                        appended++;
                    }
                    if (!string.IsNullOrEmpty(slotCode)
                        && row >= 1 && row <= report.Rows
                        && col >= 1 && col <= report.Cols)
                    {
                        report.SlotDefectCodes[row - 1, col - 1] = slotCode;
                    }
                });

                context.SetNodeOutput(Id, "AppendedCount", appended);
                return FlowResult.Ok();
            }
            catch (Exception ex) { return FlowResult.Fail($"PopulateTrayReport failed: {ex.Message}"); }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            Console.WriteLine("[SIMULATION] PopulateTrayReport: no store attached — noop");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "AppendedCount", 0);
            return FlowResult.Ok();
        }
    }
}
