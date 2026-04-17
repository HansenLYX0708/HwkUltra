using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimCommunicationUnloadNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Communication Unload (Sim)";
        public override string NodeType => "CommunicationUnload";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "LoadLock", DisplayName = "Load Lock", Type = "string", Required = true },
            new FlowParameter { Name = "EmpId", DisplayName = "Employee ID", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var loadLock = context.GetNodeInput<string>(Id, "LoadLock") ?? "L";
            Console.WriteLine($"[SIMULATION] CommunicationUnload: LoadLock={loadLock}");
            await Task.Delay(100, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
