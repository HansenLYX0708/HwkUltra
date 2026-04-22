using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>Create a named <see cref="ImagePool"/> in the SharedContext.</summary>
    public class ImagePoolCreateNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Image Pool Create";
        public override string NodeType => "ImagePoolCreate";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Name",     DisplayName = "Pool Name", Type = "string", Required = true, DefaultValue = "ImagePool" },
            new FlowParameter { Name = "Capacity", DisplayName = "Capacity",  Type = "int",    Required = false, DefaultValue = 50, Description = "Bounded queue capacity for back-pressure" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Created", DisplayName = "Created", Type = "bool" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var name = context.GetNodeInput<string>(Id, "Name");
                if (string.IsNullOrWhiteSpace(name)) name = "ImagePool";
                var capacity = context.GetNodeInput<int>(Id, "Capacity");
                if (capacity <= 0) capacity = 50;

                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("ImagePoolCreate requires SharedContext"));

                // If a pool with the same name already exists, dispose and replace.
                var existing = context.SharedContext.GetPool(name!);
                if (existing != null)
                {
                    existing.Dispose();
                    context.SharedContext.RemoveVariable(name!);
                }

                context.SharedContext.CreatePool(name!, capacity);
                context.SetNodeOutput(Id, "Created", true);
                Console.WriteLine($"[ImagePoolCreate] {name} capacity={capacity}");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"ImagePoolCreate failed: {ex.Message}"));
            }
        }
    }
}
