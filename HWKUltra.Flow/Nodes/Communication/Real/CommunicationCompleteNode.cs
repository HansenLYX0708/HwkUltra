using HWKUltra.Communication.Abstractions;
using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Report inspection completion with defect data to the host.
    /// Reads defect slider info from shared variables (JSON string).
    /// </summary>
    public class CommunicationCompleteNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "Communication Complete";
        public override string NodeType => "CommunicationComplete";
        protected override int SimulatedDelayMs => 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "TrayId", DisplayName = "Tray ID", Type = "string", Required = true, Description = "Tray serial number" },
            new FlowParameter { Name = "LoadLock", DisplayName = "Load Lock", Type = "string", Required = true, Description = "Load lock identifier (L/R)" },
            new FlowParameter { Name = "EmpId", DisplayName = "Employee ID", Type = "string", Required = true, Description = "Operator employee ID" },
            new FlowParameter { Name = "DefectCount", DisplayName = "Defect Count", Type = "int", Required = false, Description = "Number of defect sliders (0 = no defects)" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public CommunicationCompleteNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var trayId = context.GetNodeInput<string>(Id, "TrayId") ?? "";
                var loadLock = context.GetNodeInput<string>(Id, "LoadLock") ?? "";
                var empId = context.GetNodeInput<string>(Id, "EmpId") ?? "";

                var data = new CommunicationCompleteData
                {
                    TrayId = trayId,
                    LoadLock = loadLock,
                    EmpId = empId,
                    DefectSliders = new List<SliderDefectInfo>()
                };

                Service!.CompleteRequest(data);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Communication Complete failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var trayId = context.GetNodeInput<string>(Id, "TrayId") ?? "SIM-TRAY";
            Console.WriteLine($"[SIMULATION] CommunicationComplete: TrayId={trayId}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
