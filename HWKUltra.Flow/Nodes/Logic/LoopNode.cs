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

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var iterations = context.GetVariable<int>("Iterations");
                var conditionVar = context.GetVariable<string>("ConditionVariable");
                var continueValue = context.GetVariable<string>("ContinueValue") ?? "true";

                // Get current iteration from context (maintained across executions)
                var currentIteration = context.GetVariable<int>("CurrentIteration");
                var totalIterations = context.GetVariable<int>("TotalIterations");

                // Check if we should continue looping
                bool shouldContinue = true;

                // Check iteration count
                if (iterations > 0 && currentIteration >= iterations)
                {
                    shouldContinue = false;
                }

                // Check condition variable if specified
                if (!string.IsNullOrEmpty(conditionVar))
                {
                    var conditionValue = context.GetVariable<object>(conditionVar)?.ToString();
                    if (conditionValue != continueValue)
                    {
                        shouldContinue = false;
                    }
                }

                if (shouldContinue)
                {
                    Console.WriteLine($"[Loop] Iteration {currentIteration + 1}/{iterations}");

                    context.SetVariable("CurrentIteration", currentIteration + 1);
                    context.SetVariable("TotalIterations", totalIterations + 1);
                    context.SetVariable("LoopCompleted", false);

                    // Return with "Continue" to indicate loop should continue
                    return FlowResult.Ok("Continue");
                }
                else
                {
                    Console.WriteLine($"[Loop] Completed after {totalIterations} iterations");

                    context.SetVariable("LoopCompleted", true);
                    context.SetVariable("CurrentIteration", 0); // Reset for next time

                    // Return with "Exit" to indicate loop should exit
                    return FlowResult.Ok("Exit");
                }
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Loop execution failed: {ex.Message}");
            }
        }
    }
}
