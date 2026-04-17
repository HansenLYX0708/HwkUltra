using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Open the communication channel to the factory host (MES).
    /// </summary>
    public class CommunicationOpenNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "Communication Open";
        public override string NodeType => "CommunicationOpen";
        protected override int SimulatedDelayMs => 200;

        public override List<FlowParameter> Inputs { get; } = new();
        public override List<FlowParameter> Outputs { get; } = new();

        public CommunicationOpenNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                Service!.Open();
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Communication open failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            Console.WriteLine("[SIMULATION] CommunicationOpen: Connected to host");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
