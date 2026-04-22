using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Dispose an <see cref="ImagePool"/> and remove it from SharedContext.
    /// Any undelivered frames are disposed to avoid Bitmap leaks.
    /// </summary>
    public class ImagePoolCloseNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Image Pool Close";
        public override string NodeType => "ImagePoolClose";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Name", DisplayName = "Pool Name", Type = "string", Required = true, DefaultValue = "ImagePool" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Removed", DisplayName = "Removed", Type = "bool" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var name = context.GetNodeInput<string>(Id, "Name") ?? "ImagePool";
                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("ImagePoolClose requires SharedContext"));

                bool removed = context.SharedContext.RemovePool(name);
                context.SetNodeOutput(Id, "Removed", removed);
                Console.WriteLine($"[ImagePoolClose] {name} removed={removed}");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"ImagePoolClose failed: {ex.Message}"));
            }
        }
    }
}
