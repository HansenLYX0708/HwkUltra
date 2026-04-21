using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Load one image from a folder by index (for iteration). Can optionally delay
    /// <c>IntervalMs</c> before returning to simulate a camera at a fixed FPS.
    /// Use a <c>Loop</c> + incrementing counter as the index to sweep the whole folder.
    /// </summary>
    public class LoadImageFolderNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Load Image Folder";
        public override string NodeType => "LoadImageFolder";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Folder",         DisplayName = "Folder",           Type = "string", Required = true, Description = "Absolute path to directory containing images" },
            new FlowParameter { Name = "Pattern",        DisplayName = "Pattern",          Type = "string", Required = false, DefaultValue = "*.bmp;*.png;*.jpg", Description = "Semicolon-separated glob patterns" },
            new FlowParameter { Name = "Index",          DisplayName = "Index",            Type = "int",    Required = false, DefaultValue = 0, Description = "0-based index of image to load (wraps around)" },
            new FlowParameter { Name = "IntervalMs",     DisplayName = "Interval (ms)",    Type = "int",    Required = false, DefaultValue = 0, Description = "Delay before returning, simulates camera FPS (0 = no delay)" },
            new FlowParameter { Name = "OutputVariable", DisplayName = "Output Variable",  Type = "string", Required = false, DefaultValue = "LoadedBitmap", Description = "Context variable name to store the loaded Bitmap" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Count",  DisplayName = "File Count", Type = "int" },
            new FlowParameter { Name = "Path",   DisplayName = "Path",       Type = "string" },
            new FlowParameter { Name = "Width",  DisplayName = "Width",      Type = "int" },
            new FlowParameter { Name = "Height", DisplayName = "Height",     Type = "int" },
            new FlowParameter { Name = "IsLast", DisplayName = "Is Last",    Type = "bool" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var folder  = context.GetNodeInput<string>(Id, "Folder") ?? "";
                var pattern = context.GetNodeInput<string>(Id, "Pattern");
                if (string.IsNullOrWhiteSpace(pattern)) pattern = "*.bmp;*.png;*.jpg";
                var index   = context.GetNodeInput<int>(Id, "Index");
                var delay   = context.GetNodeInput<int>(Id, "IntervalMs");
                var outVar  = context.GetNodeInput<string>(Id, "OutputVariable");
                if (string.IsNullOrWhiteSpace(outVar)) outVar = "LoadedBitmap";

                if (!Directory.Exists(folder))
                    return FlowResult.Fail($"LoadImageFolder: folder not found: {folder}");

                var files = pattern!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                    .SelectMany(p => Directory.GetFiles(folder, p.Trim()))
                                    .Distinct()
                                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                    .ToArray();

                context.SetNodeOutput(Id, "Count", files.Length);
                if (files.Length == 0)
                    return FlowResult.Fail($"LoadImageFolder: no images matching '{pattern}' in {folder}");

                if (delay > 0)
                    await Task.Delay(delay, context.CancellationToken);

                if (index < 0) index = 0;
                int idx = index % files.Length;
                var path = files[idx];

                var bmp = new Bitmap(path);
                context.Variables[outVar!] = bmp;
                context.Variables[outVar + "_Path"] = path;
                context.Variables[outVar + "_Index"] = idx;

                context.SetNodeOutput(Id, "Path", path);
                context.SetNodeOutput(Id, "Width", bmp.Width);
                context.SetNodeOutput(Id, "Height", bmp.Height);
                context.SetNodeOutput(Id, "IsLast", idx == files.Length - 1);

                Console.WriteLine($"[LoadImageFolder] [{idx}/{files.Length - 1}] {path} -> {outVar} ({bmp.Width}x{bmp.Height})");
                return FlowResult.Ok();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                return FlowResult.Fail($"LoadImageFolder failed: {ex.Message}");
            }
        }
    }
}
