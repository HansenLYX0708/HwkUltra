using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Authenticate a user against the factory host system.
    /// </summary>
    public class CommunicationLoginNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "Communication Login";
        public override string NodeType => "CommunicationLogin";
        protected override int SimulatedDelayMs => 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "UserId", DisplayName = "User ID", Type = "string", Required = true, Description = "User login ID" },
            new FlowParameter { Name = "Password", DisplayName = "Password", Type = "string", Required = true, Description = "User password" }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public CommunicationLoginNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var userId = context.GetNodeInput<string>(Id, "UserId") ?? "";
                var password = context.GetNodeInput<string>(Id, "Password") ?? "";

                Service!.UserAuthentication(userId, password);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Communication Login failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var userId = context.GetNodeInput<string>(Id, "UserId") ?? "SIM-USER";
            Console.WriteLine($"[SIMULATION] CommunicationLogin: UserId={userId}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
