using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Clear list node - clears a list in SharedFlowContext.
    /// </summary>
    public class ClearListNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Clear List";
        public override string NodeType => "ClearList";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Key", DisplayName = "Variable Key", Type = "string", Required = true, Description = "List variable name to clear" }
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

                if (string.IsNullOrEmpty(key))
                    return Task.FromResult(FlowResult.Fail("Key is required"));

                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("SharedContext is not available."));

                var list = context.SharedContext.GetVariable<List<Dictionary<string, object>>>(key);
                if (list != null)
                {
                    int count = list.Count;
                    list.Clear();
                    Console.WriteLine($"[ClearList] Cleared '{key}' (had {count} items)");
                }
                else
                {
                    Console.WriteLine($"[ClearList] List '{key}' not found, creating empty list");
                    context.SharedContext.SetVariable(key, new List<Dictionary<string, object>>());
                }

                context.SetNodeOutput(Id, "Success", true);
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"ClearList failed: {ex.Message}"));
            }
        }
    }
}
