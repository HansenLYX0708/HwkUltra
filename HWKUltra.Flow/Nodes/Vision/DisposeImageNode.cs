using System.Drawing;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Vision
{
    /// <summary>
    /// Release a Bitmap / PoolItem held in a FlowContext variable. Typically placed at
    /// the end of a worker sub-flow to free resources after per-frame processing.
    /// </summary>
    public class DisposeImageNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Dispose Image";
        public override string NodeType => "DisposeImage";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Variable", DisplayName = "Variable", Type = "string", Required = true, DefaultValue = "CurrentItem", Description = "Context variable holding Bitmap or PoolItem" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Disposed", DisplayName = "Disposed", Type = "bool" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var varName = context.GetNodeInput<string>(Id, "Variable");
                if (string.IsNullOrWhiteSpace(varName)) varName = "CurrentItem";

                var obj = context.FindVariable<object>(varName!);
                bool disposed = false;
                switch (obj)
                {
                    case PoolItem pi: pi.Dispose(); disposed = true; break;
                    case Bitmap bmp:  bmp.Dispose(); disposed = true; break;
                    case IDisposable d: d.Dispose(); disposed = true; break;
                }

                context.SetNodeOutput(Id, "Disposed", disposed);
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"DisposeImage failed: {ex.Message}"));
            }
        }
    }
}
