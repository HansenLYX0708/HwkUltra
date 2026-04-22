using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Enumerate files in a folder and store the resulting path list into a shared
    /// variable. Designed to feed the enhanced <c>ParallelNode</c> via <c>ItemsSource</c>,
    /// but also usable for any collection-based downstream processing.
    /// </summary>
    public class EnumerateFolderNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Enumerate Folder";
        public override string NodeType => "EnumerateFolder";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Folder",         DisplayName = "Folder",          Type = "string", Required = true,  Description = "Absolute path to directory containing files" },
            new FlowParameter { Name = "Pattern",        DisplayName = "Pattern",         Type = "string", Required = false, DefaultValue = "*.bmp;*.png;*.jpg;*.jpeg;*.tiff", Description = "Semicolon-separated glob patterns" },
            new FlowParameter { Name = "Recursive",      DisplayName = "Recursive",       Type = "bool",   Required = false, DefaultValue = false, Description = "Include subdirectories" },
            new FlowParameter { Name = "SortBy",         DisplayName = "Sort By",         Type = "string", Required = false, DefaultValue = "Name", Description = "Name | Date | None" },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable", Type = "string", Required = false, DefaultValue = "FileList", Description = "Shared variable name to receive List<string> of paths" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Count",     DisplayName = "Count",      Type = "int" },
            new FlowParameter { Name = "FirstFile", DisplayName = "First File", Type = "string" },
            new FlowParameter { Name = "LastFile",  DisplayName = "Last File",  Type = "string" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var folder    = context.GetNodeInput<string>(Id, "Folder") ?? "";
                var pattern   = context.GetNodeInput<string>(Id, "Pattern");
                if (string.IsNullOrWhiteSpace(pattern)) pattern = "*.bmp;*.png;*.jpg;*.jpeg;*.tiff";
                var recursiveStr = context.GetNodeInput<string>(Id, "Recursive");
                bool recursive  = recursiveStr != null && recursiveStr.Equals("true", StringComparison.OrdinalIgnoreCase);
                var sortBy    = context.GetNodeInput<string>(Id, "SortBy") ?? "Name";
                var outVar    = context.GetNodeInput<string>(Id, "OutputVariable");
                if (string.IsNullOrWhiteSpace(outVar)) outVar = "FileList";

                if (!Directory.Exists(folder))
                    return Task.FromResult(FlowResult.Fail($"EnumerateFolder: folder not found: {folder}"));

                var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = pattern!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                    .SelectMany(p => Directory.GetFiles(folder, p.Trim(), opt))
                                    .Distinct()
                                    .ToList();

                files = sortBy.ToLowerInvariant() switch
                {
                    "date" => files.OrderBy(f => File.GetLastWriteTime(f)).ToList(),
                    "none" => files,
                    _      => files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList()
                };

                // Store into shared context if available; fall back to local variables.
                if (context.SharedContext != null)
                    context.SharedContext.SetVariable(outVar!, files);
                else
                    context.Variables[outVar!] = files;

                context.SetNodeOutput(Id, "Count", files.Count);
                context.SetNodeOutput(Id, "FirstFile", files.Count > 0 ? files[0] : "");
                context.SetNodeOutput(Id, "LastFile",  files.Count > 0 ? files[^1] : "");

                Console.WriteLine($"[EnumerateFolder] {files.Count} file(s) matching '{pattern}' in {folder} -> {outVar}");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"EnumerateFolder failed: {ex.Message}"));
            }
        }
    }
}
