using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Simulation
{
    /// <summary>
    /// Simulated measurement get data node.
    /// </summary>
    public class SimMeasurementGetDataNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Measurement GetData (Sim)";
        public override string NodeType => "MeasurementGetData";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Value (mm)", Type = "double" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] MeasurementGetData: Read value from {name}");
            await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "Value", 0.1234);
            return FlowResult.Ok();
        }
    }
}
