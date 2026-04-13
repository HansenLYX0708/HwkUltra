using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Nodes
{
    /// <summary>
    /// AOI inspection node - image processing and defect detection
    /// </summary>
    public class InspectionNode : IFlowNode
    {
        // TODO: Inject inspection service
        // private readonly IInspectionService _inspectionService;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "AOI Inspection";
        public string NodeType => "Inspection";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "ImageData", DisplayName = "Image Data", Type = "image", Required = true, Description = "Input image" },
            new FlowParameter { Name = "RecipeName", DisplayName = "Recipe Name", Type = "string", Required = true, Description = "Inspection recipe name" },
            new FlowParameter { Name = "Region", DisplayName = "Region", Type = "region", Required = false, Description = "ROI region" }
        };

        public List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Result", DisplayName = "Result", Type = "bool", Description = "Pass/Fail" },
            new FlowParameter { Name = "DefectCount", DisplayName = "Defect Count", Type = "int" },
            new FlowParameter { Name = "Defects", DisplayName = "Defects", Type = "array", Description = "Defect details" },
            new FlowParameter { Name = "ProcessingTime", DisplayName = "Processing Time", Type = "double", Description = "Milliseconds" }
        };

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var recipeName = context.GetVariable<string>("RecipeName") ?? "Default";
                var startTime = DateTime.Now;

                Console.WriteLine($"[Inspection] Starting inspection, recipe: {recipeName}");

                // TODO: Actually call inspection algorithm
                // var result = await _inspectionService.InspectAsync(image, recipeName);

                // Simulate inspection
                await Task.Delay(200);

                var isOk = true; // Simulated result
                var defectCount = 0;

                var processingTime = (DateTime.Now - startTime).TotalMilliseconds;

                // Set outputs
                context.SetVariable("Result", isOk);
                context.SetVariable("DefectCount", defectCount);
                context.SetVariable("Defects", new List<object>());
                context.SetVariable("ProcessingTime", processingTime);

                Console.WriteLine($"[Inspection] Inspection completed, result: {(isOk ? "OK" : "NG")}, time: {processingTime:F1}ms");

                // Branch based on inspection result
                return isOk ? FlowResult.Ok("OK") : FlowResult.Ok("NG");
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Inspection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Laser measurement node
    /// </summary>
    public class LaserMeasureNode : IFlowNode
    {
        // TODO: Inject laser service

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Laser Measurement";
        public string NodeType => "Laser";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "LaserId", DisplayName = "Laser ID", Type = "string", Required = true },
            new FlowParameter { Name = "TriggerMode", DisplayName = "Trigger Mode", Type = "string", Required = false, DefaultValue = "Continuous" },
            new FlowParameter { Name = "AverageCount", DisplayName = "Average Count", Type = "int", Required = false, DefaultValue = 1 }
        };

        public List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Height", DisplayName = "Height", Type = "double", Description = "Measured height (mm)" },
            new FlowParameter { Name = "Intensity", DisplayName = "Intensity", Type = "double" }
        };

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var laserId = context.GetVariable<string>("LaserId") ?? "Laser1";
                var averageCount = context.GetVariable<int>("AverageCount");

                Console.WriteLine($"[Laser] Laser {laserId} measurement started...");

                // Simulate measurement
                await Task.Delay(50);

                var height = 0.5 + (new Random().NextDouble() * 0.01); // Simulated height

                context.SetVariable("Height", height);
                context.SetVariable("Intensity", 1000.0);

                Console.WriteLine($"[Laser] Measurement completed: {height:F4}mm");

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Laser measurement failed: {ex.Message}");
            }
        }
    }
}
