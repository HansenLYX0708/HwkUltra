using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Abort the current operation on the factory host.
    /// </summary>
    public class CommunicationAbortNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "Communication Abort";
        public override string NodeType => "CommunicationAbort";
        protected override int SimulatedDelayMs => 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "TrayId", DisplayName = "Tray ID", Type = "string", Required = true, Description = "Tray serial number" },
            new FlowParameter { Name = "LoadLock", DisplayName = "Load Lock", Type = "string", Required = true, Description = "Load lock identifier (L/R)" },
            new FlowParameter { Name = "EmpId", DisplayName = "Employee ID", Type = "string", Required = true, Description = "Operator employee ID" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public CommunicationAbortNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var trayId = context.GetNodeInput<string>(Id, "TrayId") ?? "";
                var loadLock = context.GetNodeInput<string>(Id, "LoadLock") ?? "";
                var empId = context.GetNodeInput<string>(Id, "EmpId") ?? "";

                Service!.Abort(trayId, loadLock, empId);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Communication Abort failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var trayId = context.GetNodeInput<string>(Id, "TrayId") ?? "SIM-TRAY";
            Console.WriteLine($"[SIMULATION] CommunicationAbort: TrayId={trayId}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
