using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimCommunicationOpenNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Communication Open (Sim)";
        public override string NodeType => "CommunicationOpen";

        public override List<FlowParameter> Inputs { get; } = new();
        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            Console.WriteLine("[SIMULATION] CommunicationOpen: Connected to host");
            await Task.Delay(200, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
