using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Request host to load a tray.
    /// </summary>
    public class CommunicationLoadNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "Communication Load";
        public override string NodeType => "CommunicationLoad";
        protected override int SimulatedDelayMs => 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "LoadLock", DisplayName = "Load Lock", Type = "string", Required = true, Description = "Load lock identifier (L/R)" },
            new FlowParameter { Name = "EmpId", DisplayName = "Employee ID", Type = "string", Required = true, Description = "Operator employee ID" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public CommunicationLoadNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var loadLock = context.GetNodeInput<string>(Id, "LoadLock") ?? "";
                var empId = context.GetNodeInput<string>(Id, "EmpId") ?? "";

                Service!.Load(loadLock, empId);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Communication Load failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var loadLock = context.GetNodeInput<string>(Id, "LoadLock") ?? "L";
            Console.WriteLine($"[SIMULATION] CommunicationLoad: LoadLock={loadLock}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
