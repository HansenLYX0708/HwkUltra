using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.Flow.Nodes.Motion.Real
{
    /// <summary>
    /// Absolute position motion node - moves axis to target position
    /// </summary>
    public class AxisMoveAbsNode : DeviceNodeBase<IMotionController>
    {
        public override string Name { get; set; } = "Axis Move Absolute";
        public override string NodeType => "AxisMoveAbs";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "Position", DisplayName = "Target Position", Type = "double", Required = true, Description = "Target position (mm)" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 50000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Acceleration" },
            new FlowParameter { Name = "Deceleration", DisplayName = "Deceleration", Type = "double", Required = false, DefaultValue = 1000000.0, Description = "Deceleration" },
            new FlowParameter { Name = "WaitForComplete", DisplayName = "Wait For Complete", Type = "bool", Required = false, DefaultValue = true, Description = "Wait for motion to complete" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualPosition", DisplayName = "Actual Position", Type = "double", Description = "Actual position after motion" },
            new FlowParameter { Name = "CommandPosition", DisplayName = "Command Position", Type = "double", Description = "Commanded position" }
        };

        public AxisMoveAbsNode(IMotionController? motionController) : base(motionController) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var position = context.GetVariable<double>("Position");
                var velocity = context.GetVariable<double>("Velocity");
                var acceleration = context.GetVariable<double>("Acceleration");
                var deceleration = context.GetVariable<double>("Deceleration");
                var waitForComplete = context.GetVariable<bool>("WaitForComplete");

                if (IsSimulated)
                {
                    Console.WriteLine($"[AxisMoveAbs] Simulating move {axisName} to {position:F3}mm");
                    await Task.Delay(100, context.CancellationToken);
                    context.SetVariable("ActualPosition", position);
                    context.SetVariable("CommandPosition", position);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                var profile = new MotionProfile
                {
                    Vel = (float)velocity,
                    Acc = (float)acceleration,
                    Dec = (float)deceleration
                };

                Service!.MoveAxis(axisName, position, profile);

                if (waitForComplete)
                {
                    // TODO: Wait for motion complete
                    // Note: IsBusy requires axis ID (int), not axis name
                    // while (Service.IsBusy(axisId))
                    // {
                    //     await Task.Delay(10, context.CancellationToken);
                    // }
                    await Task.Delay(100, context.CancellationToken);
                }

                context.SetVariable("ActualPosition", position);
                context.SetVariable("CommandPosition", position);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Absolute motion failed: {ex.Message}");
            }
        }
    }
}
