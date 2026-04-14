using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Core;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Axis homing node - performs home operation on a single axis
    /// </summary>
    public class AxisHomeNode : DeviceNodeBase<MotionRouter>
    {
        public override string Name { get; set; } = "Axis Home";
        public override string NodeType => "AxisHome";
        protected override int SimulatedDelayMs => 500;

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

        public AxisHomeNode(MotionRouter? router) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";

                Service!.Home(axisName);
                await Service.WaitForIdleAsync(axisName, 30000, context.CancellationToken);

                context.SetNodeOutput(Id, "HomeCompleted", true);
                context.SetNodeOutput(Id, "HomePosition", Service.GetPosition(axisName));

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Axis homing failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var axisName = context.GetNodeInput<string>(Id, "AxisName") ?? "X";
            Console.WriteLine($"[SIMULATION] AxisHome: Homing axis {axisName}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "HomeCompleted", true);
            context.SetNodeOutput(Id, "HomePosition", 0.0);
            return FlowResult.Ok();
        }
    }
}
