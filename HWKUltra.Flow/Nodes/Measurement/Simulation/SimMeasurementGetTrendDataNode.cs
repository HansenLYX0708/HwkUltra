using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Simulation
{
    /// <summary>
    /// Simulated measurement get trend data node.
    /// </summary>
    public class SimMeasurementGetTrendDataNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Measurement GetTrendData (Sim)";
        public override string NodeType => "MeasurementGetTrendData";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true },
            new FlowParameter { Name = "StartIndex", DisplayName = "Start Index", Type = "int", Required = true },
            new FlowParameter { Name = "EndIndex", DisplayName = "End Index", Type = "int", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Data", DisplayName = "Data", Type = "double[]" },
            new FlowParameter { Name = "Count", DisplayName = "Count", Type = "int" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] MeasurementGetTrendData: Get trend from {name}");
            await Task.Delay(100, context.CancellationToken);
            context.SetNodeOutput(Id, "Data", new double[] { 0.1, 0.2, 0.3 });
            context.SetNodeOutput(Id, "Count", 3);
            return FlowResult.Ok();
        }
    }
}
