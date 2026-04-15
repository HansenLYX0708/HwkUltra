using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Set signal node - sets a named signal in SharedFlowContext,
    /// waking up any parallel flows waiting on that signal.
    /// </summary>
    public class SetSignalNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Set Signal";
        public override string NodeType => "SetSignal";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "SignalName", DisplayName = "Signal Name", Type = "string", Required = true, Description = "Name of the signal to set" },
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "string", Required = false, Description = "Optional value to pass with the signal" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "SignalSet", DisplayName = "Signal Set", Type = "bool", Description = "Always true" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var signalName = context.GetNodeInput<string>(Id, "SignalName") ?? "";
                var value = context.GetNodeInput<string>(Id, "Value");

                if (string.IsNullOrEmpty(signalName))
                    return Task.FromResult(FlowResult.Fail("SignalName is required"));

                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("SharedContext is not available. SetSignal requires parallel execution context."));

                context.SharedContext.SetSignal(signalName, value);
                context.SetNodeOutput(Id, "SignalSet", true);

                Console.WriteLine($"[SetSignal] Signal '{signalName}' set with value='{value}'");

                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"SetSignal failed: {ex.Message}"));
            }
        }
    }
}
