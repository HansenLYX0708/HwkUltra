using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Get shared variable node - reads a variable from SharedFlowContext
    /// and stores it in the local flow context.
    /// </summary>
    public class GetSharedVariableNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Get Shared Variable";
        public override string NodeType => "GetSharedVariable";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Key", DisplayName = "Variable Key", Type = "string", Required = true, Description = "Shared variable name to read" },
            new FlowParameter { Name = "DefaultValue", DisplayName = "Default Value", Type = "string", Required = false, Description = "Default value if variable not found" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "string", Description = "Retrieved value" },
            new FlowParameter { Name = "Found", DisplayName = "Found", Type = "bool", Description = "Whether the variable was found" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var key = context.GetNodeInput<string>(Id, "Key") ?? "";
                var defaultValue = context.GetNodeInput<string>(Id, "DefaultValue") ?? "";

                if (string.IsNullOrEmpty(key))
                    return Task.FromResult(FlowResult.Fail("Key is required"));

                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("SharedContext is not available."));

                var found = context.SharedContext.TryGetVariable<object>(key, out var value);
                var resultValue = found && value != null ? value.ToString() ?? "" : defaultValue;

                context.SetNodeOutput(Id, "Value", resultValue);
                context.SetNodeOutput(Id, "Found", found);

                // Also store in local context for downstream nodes
                context.SetVariable(key, resultValue);

                Console.WriteLine($"[GetSharedVar] {key} = {resultValue} (found={found})");

                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"GetSharedVariable failed: {ex.Message}"));
            }
        }
    }
}
