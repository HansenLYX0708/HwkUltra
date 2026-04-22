using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Explicitly mark an <see cref="ImagePool"/> as having no more producers.
    /// Consumers blocked on the pool will drain remaining items then exit.
    /// </summary>
    public class ImagePoolCompleteNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Image Pool Complete";
        public override string NodeType => "ImagePoolComplete";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Name", DisplayName = "Pool Name", Type = "string", Required = true, DefaultValue = "ImagePool" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "TotalEnqueued", DisplayName = "Total Enqueued", Type = "int" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var name = context.GetNodeInput<string>(Id, "Name") ?? "ImagePool";
                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("ImagePoolComplete requires SharedContext"));

                var pool = context.SharedContext.GetPool(name);
                if (pool == null)
                    return Task.FromResult(FlowResult.Fail($"ImagePool '{name}' not found"));

                pool.CompleteAdding();
                context.SetNodeOutput(Id, "TotalEnqueued", (int)pool.TotalEnqueued);
                Console.WriteLine($"[ImagePoolComplete] {name} totalEnqueued={pool.TotalEnqueued}");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"ImagePoolComplete failed: {ex.Message}"));
            }
        }
    }
}
