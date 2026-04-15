using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Wait for signal node - blocks until a named signal is set by another flow.
    /// Used for cross-flow synchronization (e.g., safety checks between stages).
    /// </summary>
    public class WaitForSignalNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Wait For Signal";
        public override string NodeType => "WaitForSignal";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "SignalName", DisplayName = "Signal Name", Type = "string", Required = true, Description = "Name of the signal to wait for" },
            new FlowParameter { Name = "TimeoutMs", DisplayName = "Timeout (ms)", Type = "int", Required = false, DefaultValue = 0, Description = "Wait timeout in ms (0 = infinite)" },
            new FlowParameter { Name = "AutoReset", DisplayName = "Auto Reset", Type = "bool", Required = false, DefaultValue = false, Description = "Reset signal after receiving it" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Signal Value", Type = "string", Description = "Value passed with the signal" },
            new FlowParameter { Name = "TimedOut", DisplayName = "Timed Out", Type = "bool", Description = "True if wait timed out" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var signalName = context.GetNodeInput<string>(Id, "SignalName") ?? "";
                var timeoutMs = context.GetNodeInput<int>(Id, "TimeoutMs");
                var autoReset = context.GetNodeInput<string>(Id, "AutoReset") == "true";

                if (string.IsNullOrEmpty(signalName))
                    return FlowResult.Fail("SignalName is required");

                if (context.SharedContext == null)
                    return FlowResult.Fail("SharedContext is not available. WaitForSignal requires parallel execution context.");

                Console.WriteLine($"[WaitForSignal] Waiting for signal '{signalName}'...");

                var effectiveTimeout = timeoutMs > 0 ? timeoutMs : -1;
                object? signalValue = null;
                bool timedOut = false;

                try
                {
                    signalValue = await context.SharedContext.WaitForSignalAsync(signalName, effectiveTimeout, context.CancellationToken);
                }
                catch (OperationCanceledException) when (!context.CancellationToken.IsCancellationRequested)
                {
                    timedOut = true;
                }

                if (autoReset && !timedOut)
                {
                    context.SharedContext.ResetSignal(signalName);
                }

                context.SetNodeOutput(Id, "Value", signalValue?.ToString() ?? "");
                context.SetNodeOutput(Id, "TimedOut", timedOut);

                Console.WriteLine(timedOut
                    ? $"[WaitForSignal] Timed out waiting for '{signalName}'"
                    : $"[WaitForSignal] Received signal '{signalName}' = '{signalValue}'");

                // Branch: TimedOut or Received
                return timedOut ? FlowResult.OkBranch("TimedOut") : FlowResult.OkBranch("Received");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"WaitForSignal failed: {ex.Message}");
            }
        }
    }
}
