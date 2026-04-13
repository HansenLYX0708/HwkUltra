using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Laser.Simulated
{
    /// <summary>
    /// Simulated laser measurement node - for testing without hardware
    /// </summary>
    public class SimulatedLaserNode : LogicNodeBase, ISimulatedNode
    {
        public override string Name { get; set; } = "Simulated Laser";
        public override string NodeType => "Laser";

        public bool SimulateExecution { get; set; } = true;
        public int SimulatedDelayMs { get; set; } = 50;

        private static readonly Random _random = new Random();

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "LaserId", DisplayName = "Laser ID", Type = "string", Required = true, Description = "Laser identifier" },
            new FlowParameter { Name = "AverageCount", DisplayName = "Average Count", Type = "int", Required = false, DefaultValue = 1, Description = "Number of measurements to average" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "double", Description = "Measured height (mm)" },
            new FlowParameter { Name = "Intensity", DisplayName = "Intensity", Type = "double", Description = "Signal intensity" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var laserId = context.GetVariable<string>("LaserId") ?? "Laser1";
                var averageCount = context.GetVariable<int>("AverageCount");

                LogSimulation($"Measuring with laser {laserId}, averaging {averageCount} samples");
                await Task.Delay(SimulatedDelayMs * averageCount, context.CancellationToken);

                // Generate simulated measurement with some variation
                var baseHeight = 0.5;
                var variation = (_random.NextDouble() - 0.5) * 0.02; // +/- 0.01mm variation
                var height = baseHeight + variation;
                var intensity = 800 + _random.Next(400);

                context.SetVariable("Height", height);
                context.SetVariable("Intensity", (double)intensity);

                LogSimulation($"Measurement completed: Height={height:F4}mm, Intensity={intensity}");

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Simulated laser measurement failed: {ex.Message}");
            }
        }

        public void LogSimulation(string activity)
        {
            Console.WriteLine($"[SIMULATION] {Name}: {activity}");
        }
    }
}
