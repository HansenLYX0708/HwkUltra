using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Acquire lock node - acquires a named mutex for exclusive access to a shared resource.
    /// Used for safety interlock (e.g., only one stage can be in the inspection zone).
    /// </summary>
    public class AcquireLockNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Acquire Lock";
        public override string NodeType => "AcquireLock";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "LockName", DisplayName = "Lock Name", Type = "string", Required = true, Description = "Name of the lock to acquire (e.g., InspectionZone)" },
            new FlowParameter { Name = "TimeoutMs", DisplayName = "Timeout (ms)", Type = "int", Required = false, DefaultValue = 0, Description = "Wait timeout in ms (0 = infinite)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Acquired", DisplayName = "Acquired", Type = "bool", Description = "Whether the lock was acquired" },
            new FlowParameter { Name = "TimedOut", DisplayName = "Timed Out", Type = "bool", Description = "True if acquire timed out" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var lockName = context.GetNodeInput<string>(Id, "LockName") ?? "";
                var timeoutMs = context.GetNodeInput<int>(Id, "TimeoutMs");

                if (string.IsNullOrEmpty(lockName))
                    return FlowResult.Fail("LockName is required");

                if (context.SharedContext == null)
                    return FlowResult.Fail("SharedContext is not available. AcquireLock requires parallel execution context.");

                Console.WriteLine($"[AcquireLock] Acquiring lock '{lockName}'...");

                var effectiveTimeout = timeoutMs > 0 ? timeoutMs : -1;
                var acquired = await context.SharedContext.AcquireLockAsync(lockName, effectiveTimeout, context.CancellationToken);

                context.SetNodeOutput(Id, "Acquired", acquired);
                context.SetNodeOutput(Id, "TimedOut", !acquired);

                Console.WriteLine(acquired
                    ? $"[AcquireLock] Lock '{lockName}' acquired"
                    : $"[AcquireLock] Lock '{lockName}' timed out");

                return acquired ? FlowResult.OkBranch("Acquired") : FlowResult.OkBranch("TimedOut");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"AcquireLock failed: {ex.Message}");
            }
        }
    }
}
