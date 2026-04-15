using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Release lock node - releases a previously acquired named mutex.
    /// Must be paired with AcquireLockNode.
    /// </summary>
    public class ReleaseLockNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Release Lock";
        public override string NodeType => "ReleaseLock";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "LockName", DisplayName = "Lock Name", Type = "string", Required = true, Description = "Name of the lock to release" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Released", DisplayName = "Released", Type = "bool", Description = "Always true" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var lockName = context.GetNodeInput<string>(Id, "LockName") ?? "";

                if (string.IsNullOrEmpty(lockName))
                    return Task.FromResult(FlowResult.Fail("LockName is required"));

                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("SharedContext is not available. ReleaseLock requires parallel execution context."));

                context.SharedContext.ReleaseLock(lockName);
                context.SetNodeOutput(Id, "Released", true);

                Console.WriteLine($"[ReleaseLock] Lock '{lockName}' released");

                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"ReleaseLock failed: {ex.Message}"));
            }
        }
    }
}
