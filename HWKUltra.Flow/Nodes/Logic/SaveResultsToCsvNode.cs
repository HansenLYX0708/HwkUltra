using System.Globalization;
using System.Text;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Persist a list of dictionary rows (as produced by <see cref="AppendToListNode"/>)
    /// to a CSV file. Columns are taken from the union of all row keys, in first-seen order.
    /// Target path may contain <c>{timestamp}</c> or <c>{yyyyMMdd_HHmmss}</c> tokens.
    /// </summary>
    public class SaveResultsToCsvNode : LogicNodeBase
    {
        private static readonly object _gate = new();
        public override string Name { get; set; } = "Save Results To CSV";
        public override string NodeType => "SaveResultsToCsv";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Key",      DisplayName = "List Key",   Type = "string", Required = true,  DefaultValue = "FlowResults", Description = "Shared variable name holding List<Dictionary<string,object>>" },
            new FlowParameter { Name = "Path",     DisplayName = "Path",       Type = "string", Required = true,  DefaultValue = "results_{yyyyMMdd_HHmmss}.csv", Description = "CSV output path. Relative paths are placed under AppContext.BaseDirectory/Exports/" },
            new FlowParameter { Name = "Encoding", DisplayName = "Encoding",   Type = "string", Required = false, DefaultValue = "UTF8", Description = "UTF8 | UTF8BOM | GB2312" },
            new FlowParameter { Name = "Clear",    DisplayName = "Clear After",Type = "bool",   Required = false, DefaultValue = false, Description = "Clear the list after saving" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "FilePath", DisplayName = "File Path", Type = "string" },
            new FlowParameter { Name = "RowCount", DisplayName = "Row Count", Type = "int" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var key      = context.GetNodeInput<string>(Id, "Key") ?? "FlowResults";
                var pathRaw  = context.GetNodeInput<string>(Id, "Path") ?? "results_{yyyyMMdd_HHmmss}.csv";
                var encName  = context.GetNodeInput<string>(Id, "Encoding") ?? "UTF8";
                var clearStr = context.GetNodeInput<string>(Id, "Clear");
                bool clearAfter = clearStr != null && clearStr.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (context.SharedContext == null)
                    return FlowResult.Fail("SaveResultsToCsv requires SharedContext");

                List<Dictionary<string, object>> snapshot = new();
                int rowCount = 0;
                int retryCount = 0;
                const int maxRetries = 5;
                const int retryDelayMs = 50;

                // Retry mechanism to handle timing issues with Parallel worker completion
                while (retryCount < maxRetries)
                {
                    lock (_gate)
                    {
                        var list = context.SharedContext.GetVariable<List<Dictionary<string, object>>>(key);
                        if (list == null)
                            return FlowResult.Fail($"Shared variable '{key}' not found or not a result list");
                        snapshot = list.ToList();
                        rowCount = snapshot.Count;
                    }

                    if (rowCount > 0)
                        break;

                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                }

                Console.WriteLine($"[SaveResultsToCsv] Key='{key}', RowCount={rowCount}, ClearAfter={clearAfter}, Retries={retryCount}");

                // If list is empty after retries, skip saving and return success
                if (rowCount == 0)
                {
                    Console.WriteLine($"[SaveResultsToCsv] WARNING: List '{key}' is still empty after {maxRetries} retries, skipping CSV save");
                    context.SetNodeOutput(Id, "FilePath", "");
                    context.SetNodeOutput(Id, "RowCount", 0);
                    return FlowResult.Ok();
                }

                // Resolve path (replace tokens; make relative paths go to Exports/).
                var resolvedPath = ResolvePath(pathRaw);
                var dir = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                // Column order: union of keys in first-seen order.
                var columns = new List<string>();
                var seen = new HashSet<string>();
                foreach (var row in snapshot)
                {
                    foreach (var k in row.Keys)
                        if (seen.Add(k)) columns.Add(k);
                }

                Encoding enc;
                try
                {
                    enc = encName.ToUpperInvariant() switch
                    {
                        "UTF8BOM" => new UTF8Encoding(true),
                        "GB2312"  => GetCodePageEncoding("GB2312"),
                        _         => new UTF8Encoding(false)
                    };
                }
                catch { enc = new UTF8Encoding(false); }

                using (var writer = new StreamWriter(resolvedPath, append: false, enc))
                {
                    writer.WriteLine(string.Join(",", columns.Select(CsvEscape)));
                    foreach (var row in snapshot)
                    {
                        var cells = columns.Select(c =>
                        {
                            if (!row.TryGetValue(c, out var v) || v == null) return "";
                            return v is IFormattable fmt
                                ? fmt.ToString(null, CultureInfo.InvariantCulture)
                                : v.ToString() ?? "";
                        });
                        writer.WriteLine(string.Join(",", cells.Select(CsvEscape)));
                    }
                }

                if (clearAfter)
                {
                    lock (_gate)
                    {
                        var list = context.SharedContext.GetVariable<List<Dictionary<string, object>>>(key);
                        if (list != null)
                            list.Clear();
                    }
                }

                context.SetNodeOutput(Id, "FilePath", resolvedPath);
                context.SetNodeOutput(Id, "RowCount", snapshot.Count);
                // Expose as shared variable so UI / downstream can find the file.
                context.SharedContext.SetVariable($"{key}_CsvPath", resolvedPath);

                Console.WriteLine($"[SaveResultsToCsv] Wrote {snapshot.Count} row(s) x {columns.Count} col(s) -> {resolvedPath}");
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"SaveResultsToCsv failed: {ex.Message}");
            }
        }

        private static string ResolvePath(string raw)
        {
            var now = DateTime.Now;
            var s = raw
                .Replace("{timestamp}", now.ToString("yyyyMMdd_HHmmss"))
                .Replace("{yyyyMMdd_HHmmss}", now.ToString("yyyyMMdd_HHmmss"))
                .Replace("{yyyy-MM-dd}", now.ToString("yyyy-MM-dd"));
            if (!Path.IsPathRooted(s))
                s = Path.Combine(AppContext.BaseDirectory, "Exports", s);
            return s;
        }

        private static Encoding GetCodePageEncoding(string name)
        {
            try { return Encoding.GetEncoding(name); }
            catch (ArgumentException)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(name);
            }
        }

        private static string CsvEscape(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            bool needsQuote = v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r');
            if (!needsQuote) return v;
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
    }
}
