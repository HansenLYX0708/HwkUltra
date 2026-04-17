using HWKUltra.Communication.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Real
{
    /// <summary>
    /// Close the communication channel to the factory host.
    /// </summary>
    public class CommunicationCloseNode : DeviceNodeBase<CommunicationRouter>
    {
        public override string Name { get; set; } = "Communication Close";
        public override string NodeType => "CommunicationClose";
        protected override int SimulatedDelayMs => 100;

        public override List<FlowParameter> Inputs { get; } = new();
        public override List<FlowParameter> Outputs { get; } = new();

        public CommunicationCloseNode(CommunicationRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                Service!.Close();
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Communication close failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            Console.WriteLine("[SIMULATION] CommunicationClose: Disconnected from host");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
