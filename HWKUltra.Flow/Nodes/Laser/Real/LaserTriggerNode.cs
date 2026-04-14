using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Laser.Real
{
    /// <summary>
    /// Laser measurement trigger node - triggers laser measurement
    /// </summary>
    public class LaserTriggerNode : DeviceNodeBase<object>  // TODO: Replace with ILaserService
    {
        public override string Name { get; set; } = "Laser Trigger";
        public override string NodeType => "LaserTrigger";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "LaserId", DisplayName = "Laser ID", Type = "string", Required = true, Description = "Laser identifier" },
            new FlowParameter { Name = "TriggerMode", DisplayName = "Trigger Mode", Type = "string", Required = false, DefaultValue = "Single", Description = "Single, Continuous, Burst" },
            new FlowParameter { Name = "AverageCount", DisplayName = "Average Count", Type = "int", Required = false, DefaultValue = 1, Description = "Number of measurements to average" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "double", Description = "Measured height (mm)" },
            new FlowParameter { Name = "Intensity", DisplayName = "Intensity", Type = "double", Description = "Signal intensity" },
            new FlowParameter { Name = "MeasurementTime", DisplayName = "Measurement Time", Type = "datetime", Description = "Timestamp of measurement" }
        };

        protected override int SimulatedDelayMs => 50;

        public LaserTriggerNode(object? laserService = null) : base(laserService) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var laserId = context.GetNodeInput<string>(Id, "LaserId") ?? "Laser1";
                var triggerMode = context.GetNodeInput<string>(Id, "TriggerMode") ?? "Single";

                // TODO: Actual laser trigger
                await Task.CompletedTask;

                context.SetNodeOutput(Id, "MeasurementTime", DateTime.Now);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Laser trigger failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var laserId = context.GetNodeInput<string>(Id, "LaserId") ?? "Laser1";
            Console.WriteLine($"[SIMULATION] LaserTrigger: Trigger laser {laserId}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);

            var random = new Random();
            context.SetNodeOutput(Id, "Height", 0.5 + (random.NextDouble() * 0.01));
            context.SetNodeOutput(Id, "Intensity", (double)(800 + random.Next(400)));
            context.SetNodeOutput(Id, "MeasurementTime", DateTime.Now);
            return FlowResult.Ok();
        }
    }
}
