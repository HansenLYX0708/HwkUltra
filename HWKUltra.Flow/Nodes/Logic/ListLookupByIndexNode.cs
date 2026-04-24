using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Lookup a row in a shared list (List&lt;Dictionary&lt;string,object&gt;&gt;) by index,
    /// and expose selected columns as local flow variables so downstream nodes can consume them.
    ///
    /// <para><b>Columns</b> format: semicolon-separated <c>LocalVar=ColumnName</c> pairs.
    /// Example: <c>Row=Row;Col=Col;Idx=Index</c> maps list row columns to local variables.</para>
    /// </summary>
    public class ListLookupByIndexNode : LogicNodeBase
    {
        public override string Name { get; set; } = "List Lookup By Index";
        public override string NodeType => "ListLookupByIndex";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Key",     DisplayName = "List Key",    Type = "string", Required = true,  Description = "Shared list variable name" },
            new FlowParameter { Name = "Index",   DisplayName = "Index",       Type = "int",    Required = true,  Description = "Zero-based list index" },
            new FlowParameter { Name = "Columns", DisplayName = "Columns",     Type = "string", Required = true,  Description = "Semicolon-separated LocalVar=ColumnName pairs" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Found", DisplayName = "Found", Type = "bool" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var key = context.GetNodeInput<string>(Id, "Key") ?? "";
                var index = context.GetNodeInput<int>(Id, "Index");
                var colsRaw = context.GetNodeInput<string>(Id, "Columns") ?? "";

                if (string.IsNullOrEmpty(key))
                    return Task.FromResult(FlowResult.Fail("Key is required"));
                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("ListLookupByIndex requires SharedContext"));

                var list = context.SharedContext.GetVariable<List<Dictionary<string, object>>>(key);
                if (list == null || index < 0 || index >= list.Count)
                {
                    context.SetNodeOutput(Id, "Found", false);
                    Console.WriteLine($"[ListLookupByIndex] {key}[{index}] not found (listCount={list?.Count ?? 0})");
                    return Task.FromResult(FlowResult.Ok());
                }

                var row = list[index];
                foreach (var spec in colsRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var eq = spec.IndexOf('=');
                    if (eq <= 0) continue;
                    var localVar = spec[..eq].Trim();
                    var colName = spec[(eq + 1)..].Trim();
                    if (string.IsNullOrEmpty(localVar) || string.IsNullOrEmpty(colName)) continue;

                    if (row.TryGetValue(colName, out var v))
                    {
                        context.SetVariable(localVar, v?.ToString() ?? "");
                    }
                }

                context.SetNodeOutput(Id, "Found", true);
                Console.WriteLine($"[ListLookupByIndex] {key}[{index}] applied to local vars");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"ListLookupByIndex failed: {ex.Message}"));
            }
        }
    }
}
