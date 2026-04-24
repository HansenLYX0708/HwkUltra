using System.Drawing;
using System.Drawing.Imaging;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Persist a <see cref="Bitmap"/> (or <see cref="PoolItem"/>) held in a context variable
    /// to disk. Supports a simple path template with shared-variable substitution
    /// (e.g. <c>C:\out\{Row}_{Col}.bmp</c>). When <c>Enabled=false</c> the node is a no-op.
    /// </summary>
    public class SaveBitmapNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Save Bitmap";
        public override string NodeType => "SaveBitmap";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Variable", DisplayName = "Image Variable", Type = "string", Required = true,  DefaultValue = "CurrentItem", Description = "Context variable holding the Bitmap / PoolItem" },
            new FlowParameter { Name = "PathTemplate", DisplayName = "Path Template", Type = "string", Required = true, Description = "Output path; supports {VarName} substitution" },
            new FlowParameter { Name = "Format",   DisplayName = "Format",   Type = "string", Required = false, DefaultValue = "Bmp", Description = "Bmp | Png | Jpeg" },
            new FlowParameter { Name = "Enabled",  DisplayName = "Enabled",  Type = "bool",   Required = false, DefaultValue = true,  Description = "If false, skip the save" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "SavedPath", DisplayName = "Saved Path", Type = "string" },
            new FlowParameter { Name = "Saved",     DisplayName = "Saved",      Type = "bool" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var enabledStr = context.GetNodeInput<string>(Id, "Enabled") ?? "true";
                // Support variable indirection: if the value isn't a literal bool, look it up.
                if (!string.Equals(enabledStr, "true", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(enabledStr, "false", StringComparison.OrdinalIgnoreCase))
                {
                    var resolved = context.FindVariable<object>(enabledStr)?.ToString();
                    if (!string.IsNullOrEmpty(resolved)) enabledStr = resolved;
                }
                if (string.Equals(enabledStr, "false", StringComparison.OrdinalIgnoreCase))
                {
                    context.SetNodeOutput(Id, "Saved", false);
                    context.SetNodeOutput(Id, "SavedPath", "");
                    return Task.FromResult(FlowResult.Ok());
                }

                var varName = context.GetNodeInput<string>(Id, "Variable");
                if (string.IsNullOrWhiteSpace(varName)) varName = "CurrentItem";
                var tmpl = context.GetNodeInput<string>(Id, "PathTemplate") ?? "";
                var fmtStr = context.GetNodeInput<string>(Id, "Format") ?? "Bmp";

                if (string.IsNullOrWhiteSpace(tmpl))
                    return Task.FromResult(FlowResult.Fail("PathTemplate is required"));

                var obj = context.FindVariable<object>(varName!);
                Bitmap? bmp = obj switch
                {
                    Bitmap b => b,
                    PoolItem pi => pi.Bitmap,
                    _ => null
                };
                if (bmp == null)
                    return Task.FromResult(FlowResult.Fail($"SaveBitmap: variable '{varName}' is not a Bitmap"));

                var path = ResolvePath(tmpl, context);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var format = fmtStr.ToLowerInvariant() switch
                {
                    "png" => ImageFormat.Png,
                    "jpeg" or "jpg" => ImageFormat.Jpeg,
                    _ => ImageFormat.Bmp
                };
                bmp.Save(path, format);

                context.SetNodeOutput(Id, "Saved", true);
                context.SetNodeOutput(Id, "SavedPath", path);
                Console.WriteLine($"[SaveBitmap] -> {path}");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"SaveBitmap failed: {ex.Message}"));
            }
        }

        private static string ResolvePath(string tmpl, FlowContext ctx)
        {
            var result = tmpl;
            int idx = 0;
            while ((idx = result.IndexOf('{', idx)) >= 0)
            {
                int end = result.IndexOf('}', idx + 1);
                if (end < 0) break;
                var name = result.Substring(idx + 1, end - idx - 1);
                var value = ctx.FindVariable<object>(name)?.ToString() ?? "";
                // Handle DateTime.Now format specifier like {yyyyMMdd_HHmmss}
                if (string.IsNullOrEmpty(value) && LooksLikeDateFormat(name))
                    value = DateTime.Now.ToString(name);
                result = result.Substring(0, idx) + value + result.Substring(end + 1);
                idx += value.Length;
            }
            return result;
        }

        private static bool LooksLikeDateFormat(string s)
        {
            foreach (var c in s)
                if (!"yMdHmsfFtz_-.:".Contains(c)) return false;
            return s.Length > 0;
        }
    }
}
