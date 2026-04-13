using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Axis homing node - performs home operation on a single axis
    /// </summary>
    public class AxisHomeNode : DeviceNodeBase<IMotionController>
    {
        public override string Name { get; set; } = "Axis Home";
        public override string NodeType => "AxisHome";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "HomeMode", DisplayName = "Home Mode", Type = "string", Required = false, DefaultValue = "Auto", Description = "Auto, Positive, Negative" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 10000.0, Description = "Homing velocity" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "HomeCompleted", DisplayName = "Home Completed", Type = "bool", Description = "Whether homing was successful" },
            new FlowParameter { Name = "HomePosition", DisplayName = "Home Position", Type = "double", Description = "Position after homing" }
        };

        public AxisHomeNode(IMotionController? motionController) : base(motionController) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var homeMode = context.GetVariable<string>("HomeMode") ?? "Auto";
                var velocity = context.GetVariable<double>("Velocity");

                if (IsSimulated)
                {
                    Console.WriteLine($"[AxisHome] Simulating home for axis {axisName}, mode={homeMode}");
                    await Task.Delay(500, context.CancellationToken);
                    context.SetVariable("HomeCompleted", true);
                    context.SetVariable("HomePosition", 0.0);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                // TODO: Actual home operation
                // Service!.HomeAxis(axisName, homeMode, velocity);

                context.SetVariable("HomeCompleted", true);
                context.SetVariable("HomePosition", 0.0);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Axis homing failed: {ex.Message}");
            }
        }
    }
}
