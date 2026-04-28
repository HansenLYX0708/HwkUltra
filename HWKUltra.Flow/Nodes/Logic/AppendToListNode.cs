using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Thread-safe append of a row (Dictionary&lt;string,object&gt;) into a shared
    /// List stored in <see cref="SharedFlowContext"/>. The canonical way to collect
    /// per-item results from parallel worker sub-flows.
    ///
    /// <para><b>Columns</b> format: semicolon-separated <c>Name=Variable</c> pairs.
    /// Each variable is resolved from the current FlowContext via <c>FindVariable</c>.
    /// Example: <c>FileName=CurrentItem;Score=SharpnessScore;Idx=CurrentIndex</c>.</para>
    /// </summary>
    public class AppendToListNode : LogicNodeBase
    {
        private static readonly object _gate = new();

        public override string Name { get; set; } = "Append To List";
        public override string NodeType => "AppendToList";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Key",     DisplayName = "List Key",    Type = "string", Required = true,  DefaultValue = "FlowResults", Description = "Shared variable name holding the result list" },
            new FlowParameter { Name = "Columns", DisplayName = "Columns",     Type = "string", Required = true,  Description = "Semicolon-separated ColumnName=VariableName pairs" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "NewCount", DisplayName = "New Count", Type = "int", Description = "Length of the list after append" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var key = context.GetNodeInput<string>(Id, "Key");
                if (string.IsNullOrWhiteSpace(key)) key = "FlowResults";
                var colsRaw = context.GetNodeInput<string>(Id, "Columns") ?? "";

                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("AppendToList requires SharedContext"));

                var row = new Dictionary<string, object>();
                foreach (var spec in colsRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var eq = spec.IndexOf('=');
                    if (eq <= 0) continue;
                    var colName = spec[..eq].Trim();
                    var varName = spec[(eq + 1)..].Trim();
                    if (string.IsNullOrEmpty(colName) || string.IsNullOrEmpty(varName)) continue;

                    object? value = context.FindVariable<object>(varName);
                    // Literal fallback: if no variable exists, use the raw string after '='.
                    row[colName] = value ?? varName;
                }

                int newCount;
                lock (_gate)
                {
                    var list = context.SharedContext.GetVariable<List<Dictionary<string, object>>>(key!);
                    if (list == null)
                    {
                        list = new List<Dictionary<string, object>>();
                        context.SharedContext.SetVariable(key!, list);
                    }
                    int beforeCount = list.Count;
                    list.Add(row);
                    newCount = list.Count;
                    Console.WriteLine($"[AppendToList] Key='{key}', Before={beforeCount}, After={newCount}");
                }

                context.SetNodeOutput(Id, "NewCount", newCount);
                Console.WriteLine($"[AppendToList] Row: {string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}"))}");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"AppendToList failed: {ex.Message}"));
            }
        }
    }
}
