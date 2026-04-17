using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimCommunicationLoginNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Communication Login (Sim)";
        public override string NodeType => "CommunicationLogin";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "UserId", DisplayName = "User ID", Type = "string", Required = true },
            new FlowParameter { Name = "Password", DisplayName = "Password", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var userId = context.GetNodeInput<string>(Id, "UserId") ?? "SIM-USER";
            Console.WriteLine($"[SIMULATION] CommunicationLogin: UserId={userId}");
            await Task.Delay(100, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
