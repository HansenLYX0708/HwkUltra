using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulated
{
    /// <summary>
    /// Simulated absolute motion node - for testing without hardware
    /// </summary>
    public class SimulatedAxisMoveNode : LogicNodeBase, ISimulatedNode
    {
        public override string Name { get; set; } = "Simulated Axis Move";
        public override string NodeType => "AxisMove";

        public bool SimulateExecution { get; set; } = true;
        public int SimulatedDelayMs { get; set; } = 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true },
            new FlowParameter { Name = "Position", DisplayName = "Target Position", Type = "double", Required = true },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 50000.0 }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualPosition", DisplayName = "Actual Position", Type = "double" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var position = context.GetVariable<double>("Position");
                var velocity = context.GetVariable<double>("Velocity");

                LogSimulation($"Moving axis {axisName} to position {position:F3}mm at {velocity:F1}mm/s");

                // Calculate simulated travel time based on velocity
                int travelTime = SimulatedDelayMs;
                if (velocity > 0 && Math.Abs(position) > 0)
                {
                    travelTime = Math.Min((int)(Math.Abs(position) / velocity * 1000), SimulatedDelayMs);
                }
                // Ensure minimum delay of 10ms
                travelTime = Math.Max(travelTime, 10);
                await Task.Delay(travelTime, context.CancellationToken);

                context.SetVariable("ActualPosition", position);
                LogSimulation($"Axis {axisName} reached position {position:F3}mm");

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Simulated motion failed: {ex.Message}");
            }
        }

        public void LogSimulation(string activity)
        {
            Console.WriteLine($"[SIMULATION] {Name}: {activity}");
        }
    }
}
