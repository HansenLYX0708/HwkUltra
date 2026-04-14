using HWKUltra.Measurement.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Measurement.Real
{
    /// <summary>
    /// Measurement get trend data node - retrieve trend data within an index range.
    /// </summary>
    public class MeasurementGetTrendDataNode : DeviceNodeBase<MeasurementRouter>
    {
        public override string Name { get; set; } = "Measurement GetTrendData";
        public override string NodeType => "MeasurementGetTrendData";
        protected override int SimulatedDelayMs => 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Measurement device instance name" },
            new FlowParameter { Name = "StartIndex", DisplayName = "Start Index", Type = "int", Required = true, Description = "Start index of trend data" },
            new FlowParameter { Name = "EndIndex", DisplayName = "End Index", Type = "int", Required = true, Description = "End index of trend data" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Data", DisplayName = "Data", Type = "double[]", Description = "Array of trend data values (mm)" },
            new FlowParameter { Name = "Count", DisplayName = "Count", Type = "int", Description = "Number of data points retrieved" }
        };

        public MeasurementGetTrendDataNode(MeasurementRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                uint start = (uint)context.GetNodeInput<int>(Id, "StartIndex");
                uint end = (uint)context.GetNodeInput<int>(Id, "EndIndex");

                double[] data = Service!.GetAllTrendData(name, start, end);
                context.SetNodeOutput(Id, "Data", data);
                context.SetNodeOutput(Id, "Count", data.Length);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Measurement get trend data failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] MeasurementGetTrendData: Get trend from {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "Data", new double[] { 0.1, 0.2, 0.3 });
            context.SetNodeOutput(Id, "Count", 3);
            return FlowResult.Ok();
        }
    }
}
