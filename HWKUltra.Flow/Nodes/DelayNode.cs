using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Nodes
{
    /// <summary>
    /// Delay wait node
    /// </summary>
    public class DelayNode : IFlowNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Delay Wait";
        public string NodeType => "Delay";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Duration", DisplayName = "Duration", Type = "int", Required = true, DefaultValue = 1000, Description = "Milliseconds" }
        };

        public List<FlowParameter> Outputs { get; } = new();

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var duration = context.GetVariable<int>("Duration");
                if (duration <= 0) duration = 1000;

                Console.WriteLine($"[Delay] Waiting {duration}ms...");
                await Task.Delay(duration, context.CancellationToken);

                return FlowResult.Ok();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Delay execution failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Wait for axis in-position node
    /// </summary>
    public class WaitForAxisNode : IFlowNode
    {
        // private readonly IMotionController _motionController;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Wait For Axis";
        public string NodeType => "WaitForAxis";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true },
            new FlowParameter { Name = "Timeout", DisplayName = "Timeout", Type = "int", Required = false, DefaultValue = 30000, Description = "Milliseconds" }
        };

        public List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "IsInPosition", DisplayName = "Is In Position", Type = "bool" }
        };

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var timeout = context.GetVariable<int>("Timeout");

                Console.WriteLine($"[WaitForAxis] Waiting for axis {axisName} to be in position...");

                // TODO: Actually call motion controller to check axis status
                // while (_motionController.IsBusy(axisName) && timeout > 0)

                await Task.Delay(100, context.CancellationToken);

                context.SetVariable("IsInPosition", true);
                return FlowResult.Ok();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Wait for axis in-position failed: {ex.Message}");
            }
        }
    }
}
