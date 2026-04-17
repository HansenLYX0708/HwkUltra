using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Request host to start scanning a tray.
    /// </summary>
    public class CommunicationStartScanNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "Communication StartScan";
        public override string NodeType => "CommunicationStartScan";
        protected override int SimulatedDelayMs => 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "TrayId", DisplayName = "Tray ID", Type = "string", Required = true, Description = "Tray serial number" },
            new FlowParameter { Name = "LoadLock", DisplayName = "Load Lock", Type = "string", Required = true, Description = "Load lock identifier (L/R)" },
            new FlowParameter { Name = "EmpId", DisplayName = "Employee ID", Type = "string", Required = true, Description = "Operator employee ID" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public CommunicationStartScanNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var trayId = context.GetNodeInput<string>(Id, "TrayId") ?? "";
                var loadLock = context.GetNodeInput<string>(Id, "LoadLock") ?? "";
                var empId = context.GetNodeInput<string>(Id, "EmpId") ?? "";

                Service!.StartScan(trayId, loadLock, empId);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Communication StartScan failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var trayId = context.GetNodeInput<string>(Id, "TrayId") ?? "SIM-TRAY";
            Console.WriteLine($"[SIMULATION] CommunicationStartScan: TrayId={trayId}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
