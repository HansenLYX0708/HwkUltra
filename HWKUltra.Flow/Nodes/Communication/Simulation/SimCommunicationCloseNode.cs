using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimCommunicationCloseNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Communication Close (Sim)";
        public override string NodeType => "CommunicationClose";

        public override List<FlowParameter> Inputs { get; } = new();
        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            Console.WriteLine("[SIMULATION] CommunicationClose: Disconnected from host");
            await Task.Delay(100, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
