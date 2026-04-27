using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Loop node - repeats a section of the flow
    /// </summary>
    public class LoopNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Loop";
        public override string NodeType => "Loop";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Iterations", DisplayName = "Iterations", Type = "int", Required = false, DefaultValue = 1, Description = "Number of iterations (0 = infinite)" },
            new FlowParameter { Name = "ConditionVariable", DisplayName = "Condition Variable", Type = "string", Required = false, Description = "Variable to check for loop continuation" },
            new FlowParameter { Name = "ContinueValue", DisplayName = "Continue Value", Type = "string", Required = false, DefaultValue = "true", Description = "Value that allows loop to continue" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "CurrentIteration", DisplayName = "Current Iteration", Type = "int", Description = "Current loop iteration (0-based)" },
            new FlowParameter { Name = "TotalIterations", DisplayName = "Total Iterations", Type = "int", Description = "Total iterations completed" },
            new FlowParameter { Name = "LoopCompleted", DisplayName = "Loop Completed", Type = "bool", Description = "Whether loop completed normally" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var iterationsRaw = context.GetNodeInput<string>(Id, "Iterations") ?? "1";
                var conditionVar = context.GetNodeInput<string>(Id, "ConditionVariable");
                var continueValue = context.GetNodeInput<string>(Id, "ContinueValue") ?? "true";

                // Iterations can be a literal number OR a variable name (e.g. "CycleCount").
                int iterations;
                if (!int.TryParse(iterationsRaw, out iterations))
                {
                    // Treat as variable reference
                    var resolved = context.FindVariable<object>(iterationsRaw);
                    if (resolved != null)
                    {
                        try { iterations = Convert.ToInt32(resolved); }
                        catch { return Task.FromResult(FlowResult.Fail($"Iterations variable '{iterationsRaw}' resolved to '{resolved}' which is not an integer")); }
                    }
                    else
                    {
                        return Task.FromResult(FlowResult.Fail($"Iterations '{iterationsRaw}' is neither a number nor a defined variable"));
                    }
                }

                // Get current iteration from node output (maintained across executions)
                var currentIteration = context.GetNodeInput<int>(Id, "CurrentIteration");
                var totalIterations = context.GetNodeInput<int>(Id, "TotalIterations");

                // Check if we should continue looping
                bool shouldContinue = true;

                // Check iteration count (0 or negative = infinite)
                if (iterations > 0 && currentIteration >= iterations)
                {
                    shouldContinue = false;
                }

                // Check condition variable if specified
                if (!string.IsNullOrEmpty(conditionVar))
                {
                    var conditionValue = context.FindVariable<object>(conditionVar)?.ToString();
                    if (conditionValue != continueValue)
                    {
                        shouldContinue = false;
                    }
                }

                if (shouldContinue)
                {
                    Console.WriteLine($"[Loop] Iteration {currentIteration + 1}/{iterations}");

                    context.SetNodeOutput(Id, "CurrentIteration", currentIteration + 1);
                    context.SetNodeOutput(Id, "TotalIterations", totalIterations + 1);
                    context.SetNodeOutput(Id, "LoopCompleted", false);

                    // Return with "Continue" to indicate loop should continue
                    return Task.FromResult(FlowResult.OkBranch("Continue"));
                }
                else
                {
                    Console.WriteLine($"[Loop] Completed after {totalIterations} iterations");

                    context.SetNodeOutput(Id, "LoopCompleted", true);
                    context.SetNodeOutput(Id, "CurrentIteration", 0); // Reset for next time

                    // Return with "Exit" to indicate loop should exit
                    return Task.FromResult(FlowResult.OkBranch("Exit"));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"Loop execution failed: {ex.Message}"));
            }
        }
    }
}
