using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Set shared variable node - writes a variable to SharedFlowContext,
    /// making it accessible to all parallel flows.
    /// </summary>
    public class SetSharedVariableNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Set Shared Variable";
        public override string NodeType => "SetSharedVariable";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Key", DisplayName = "Variable Key", Type = "string", Required = true, Description = "Shared variable name" },
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "string", Required = true, Description = "Value to set" },
            new FlowParameter { Name = "SourceVariable", DisplayName = "Source Variable", Type = "string", Required = false, Description = "If set, copies value from this local variable instead of using Value" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var key = context.GetNodeInput<string>(Id, "Key") ?? "";
                var value = context.GetNodeInput<string>(Id, "Value") ?? "";
                var sourceVar = context.GetNodeInput<string>(Id, "SourceVariable") ?? "";

                if (string.IsNullOrEmpty(key))
                    return Task.FromResult(FlowResult.Fail("Key is required"));

                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("SharedContext is not available."));

                // If SourceVariable is specified, copy from local context
                if (!string.IsNullOrEmpty(sourceVar))
                {
                    var sourceValue = context.FindVariable<object>(sourceVar);
                    if (sourceValue != null)
                    {
                        context.SharedContext.SetVariable(key, sourceValue);
                        Console.WriteLine($"[SetSharedVar] {key} = {sourceValue} (from {sourceVar})");
                    }
                    else
                    {
                        context.SharedContext.SetVariable(key, value);
                    }
                }
                else
                {
                    context.SharedContext.SetVariable(key, value);
                    Console.WriteLine($"[SetSharedVar] {key} = {value}");
                }

                context.SetNodeOutput(Id, "Success", true);
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"SetSharedVariable failed: {ex.Message}"));
            }
        }
    }
}
