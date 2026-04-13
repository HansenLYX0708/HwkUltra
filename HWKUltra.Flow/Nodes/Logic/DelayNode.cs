using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Delay node - pauses flow execution for specified duration
    /// </summary>
    public class DelayNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Delay";
        public override string NodeType => "Delay";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Duration", DisplayName = "Duration", Type = "int", Required = true, DefaultValue = 1000, Description = "Delay in milliseconds" },
            new FlowParameter { Name = "CanCancel", DisplayName = "Can Cancel", Type = "bool", Required = false, DefaultValue = true, Description = "Whether delay can be cancelled" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualDelay", DisplayName = "Actual Delay", Type = "int", Description = "Actual delay in ms (may be less if cancelled)" },
            new FlowParameter { Name = "WasCancelled", DisplayName = "Was Cancelled", Type = "bool", Description = "Whether delay was cancelled" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var duration = context.GetVariable<int>("Duration");
                var canCancel = context.GetVariable<bool>("CanCancel");

                if (duration <= 0)
                {
                    duration = 1000;
                }

                Console.WriteLine($"[Delay] Waiting {duration}ms...");
                var startTime = DateTime.Now;

                try
                {
                    if (canCancel)
                    {
                        await Task.Delay(duration, context.CancellationToken);
                    }
                    else
                    {
                        await Task.Delay(duration);
                    }

                    var actualDelay = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    context.SetVariable("ActualDelay", actualDelay);
                    context.SetVariable("WasCancelled", false);

                    return FlowResult.Ok();
                }
                catch (OperationCanceledException)
                {
                    var actualDelay = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    context.SetVariable("ActualDelay", actualDelay);
                    context.SetVariable("WasCancelled", true);

                    Console.WriteLine($"[Delay] Cancelled after {actualDelay}ms");
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Delay execution failed: {ex.Message}");
            }
        }
    }
}
