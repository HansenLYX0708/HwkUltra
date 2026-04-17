using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimCommunicationStartScanNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Communication StartScan (Sim)";
        public override string NodeType => "CommunicationStartScan";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "TrayId", DisplayName = "Tray ID", Type = "string", Required = true },
            new FlowParameter { Name = "LoadLock", DisplayName = "Load Lock", Type = "string", Required = true },
            new FlowParameter { Name = "EmpId", DisplayName = "Employee ID", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var trayId = context.GetNodeInput<string>(Id, "TrayId") ?? "SIM-TRAY";
            Console.WriteLine($"[SIMULATION] CommunicationStartScan: TrayId={trayId}");
            await Task.Delay(100, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
