using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Branch node - conditional flow control with multiple outputs
    /// </summary>
    public class BranchNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Branch";
        public override string NodeType => "Branch";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Condition", DisplayName = "Condition", Type = "string", Required = true, Description = "Variable name or expression to evaluate" },
            new FlowParameter { Name = "Operator", DisplayName = "Operator", Type = "string", Required = false, DefaultValue = "Equals", Description = "Equals, GreaterThan, LessThan, Contains, etc." },
            new FlowParameter { Name = "CompareValue", DisplayName = "Compare Value", Type = "string", Required = false, Description = "Value to compare against" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Result", DisplayName = "Evaluation Result", Type = "bool", Description = "Whether condition was true" },
            new FlowParameter { Name = "TrueValue", DisplayName = "True Value", Type = "any", Description = "Value when condition is true" },
            new FlowParameter { Name = "FalseValue", DisplayName = "False Value", Type = "any", Description = "Value when condition is false" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var condition = context.GetNodeInput<string>(Id, "Condition") ?? "";
                var op = context.GetNodeInput<string>(Id, "Operator") ?? "Equals";
                var compareValue = context.GetNodeInput<string>(Id, "CompareValue") ?? "";

                // Get the value to evaluate from context (condition is a variable name, search all scopes)
                var valueToEvaluate = context.FindVariable<object>(condition);
                bool result = false;

                if (valueToEvaluate != null)
                {
                    result = op switch
                    {
                        "Equals" => valueToEvaluate.ToString()?.Equals(compareValue, StringComparison.OrdinalIgnoreCase) ?? false,
                        "NotEquals" => !valueToEvaluate.ToString()?.Equals(compareValue, StringComparison.OrdinalIgnoreCase) ?? true,
                        "GreaterThan" => Convert.ToDouble(valueToEvaluate) > Convert.ToDouble(compareValue),
                        "LessThan" => Convert.ToDouble(valueToEvaluate) < Convert.ToDouble(compareValue),
                        "True" => Convert.ToBoolean(valueToEvaluate),
                        "False" => !Convert.ToBoolean(valueToEvaluate),
                        _ => false
                    };
                }

                context.SetNodeOutput(Id, "Result", result);
                context.SetNodeOutput(Id, "TrueValue", result);
                context.SetNodeOutput(Id, "FalseValue", !result);

                Console.WriteLine($"[Branch] Condition '{condition}' evaluated to: {result}");

                // Return with branch indicator
                return Task.FromResult(result ? FlowResult.OkBranch("True") : FlowResult.OkBranch("False"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"Branch evaluation failed: {ex.Message}"));
            }
        }
    }
}
