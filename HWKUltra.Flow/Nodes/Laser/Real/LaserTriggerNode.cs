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

        public LaserTriggerNode(object? laserService = null) : base(laserService) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var laserId = context.GetVariable<string>("LaserId") ?? "Laser1";
                var triggerMode = context.GetVariable<string>("TriggerMode") ?? "Single";
                var averageCount = context.GetVariable<int>("AverageCount");

                if (IsSimulated)
                {
                    Console.WriteLine($"[LaserTrigger] Simulating {triggerMode} trigger for laser {laserId}");
                    await Task.Delay(50, context.CancellationToken);

                    // Simulate measurement data
                    var random = new Random();
                    var height = 0.5 + (random.NextDouble() * 0.01);
                    var intensity = 800 + random.Next(400);

                    context.SetVariable("Height", height);
                    context.SetVariable("Intensity", (double)intensity);
                    context.SetVariable("MeasurementTime", DateTime.Now);

                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                // TODO: Actual laser trigger

                context.SetVariable("MeasurementTime", DateTime.Now);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Laser trigger failed: {ex.Message}");
            }
        }
    }
}
