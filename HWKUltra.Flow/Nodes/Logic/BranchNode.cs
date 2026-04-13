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

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var condition = context.GetVariable<string>("Condition") ?? "";
                var op = context.GetVariable<string>("Operator") ?? "Equals";
                var compareValue = context.GetVariable<string>("CompareValue") ?? "";

                // Get the value to evaluate from context
                var valueToEvaluate = context.GetVariable<object>(condition);
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

                context.SetVariable("Result", result);
                context.SetVariable("TrueValue", result);
                context.SetVariable("FalseValue", !result);

                Console.WriteLine($"[Branch] Condition '{condition}' evaluated to: {result}");

                // Return with branch indicator
                return result ? FlowResult.Ok("True") : FlowResult.Ok("False");
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Branch evaluation failed: {ex.Message}");
            }
        }
    }
}
