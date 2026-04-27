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
            new FlowParameter { Name = "CompareValue", DisplayName = "Compare Value", Type = "string", Required = false, Description = "Value to compare against" },
            new FlowParameter { Name = "SourceNodeId", DisplayName = "Source Node ID", Type = "string", Required = false, Description = "Node ID to read variable from (optional, uses FindVariable if not specified)" }
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
                var compareValueRaw = context.GetNodeInput<string>(Id, "CompareValue") ?? "";
                var sourceNodeId = context.GetNodeInput<string>(Id, "SourceNodeId");

                // Get the value to evaluate from context
                object? valueToEvaluate;
                if (!string.IsNullOrEmpty(sourceNodeId))
                {
                    // Read from specific node
                    valueToEvaluate = context.GetNodeOutput<object>(sourceNodeId, condition);
                    Console.WriteLine($"[Branch] Node '{Id}' reading from node '{sourceNodeId}', variable '{condition}', found: {valueToEvaluate?.ToString() ?? "null"}");
                }
                else
                {
                    // Use global search
                    valueToEvaluate = context.FindVariable<object>(condition);
                    Console.WriteLine($"[Branch] Node '{Id}' looking for variable '{condition}', found: {valueToEvaluate?.ToString() ?? "null"}");
                }

                // CompareValue: support literal OR variable name.
                // Heuristic: if the literal cannot be parsed to double/bool AND a variable with that name exists, use the variable value.
                string compareValue = compareValueRaw;
                bool needsNumeric = op is "GreaterThan" or "LessThan";
                bool needsBool = op is "True" or "False";
                bool literalLooksNumeric = double.TryParse(compareValueRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _);
                bool literalLooksBool = bool.TryParse(compareValueRaw, out _);
                if ((needsNumeric && !literalLooksNumeric) || (needsBool && !literalLooksBool))
                {
                    var resolved = context.FindVariable<object>(compareValueRaw);
                    if (resolved != null) compareValue = resolved.ToString() ?? "";
                }
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
