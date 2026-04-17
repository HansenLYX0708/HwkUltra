using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.TeachData.Simulation
{
    /// <summary>
    /// Simulated GetTeachPosition node — returns zeros.
    /// </summary>
    public class SimGetTeachPositionNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Get Teach Position (Sim)";
        public override string NodeType => "GetTeachPosition";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Required = true, Description = "Teach position name" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "X", DisplayName = "X", Type = "double", Description = "X axis value" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "double", Description = "Y axis value" },
            new FlowParameter { Name = "Z", DisplayName = "Z", Type = "double", Description = "Z axis value" },
            new FlowParameter { Name = "Found", DisplayName = "Found", Type = "bool", Description = "Whether the position was found" },
            new FlowParameter { Name = "AxisCount", DisplayName = "Axis Count", Type = "int", Description = "Number of axes" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var posName = context.GetNodeInput<string>(Id, "PositionName") ?? "(unknown)";
            Console.WriteLine($"[SIMULATION] GetTeachPosition: Reading '{posName}'");
            context.SetNodeOutput(Id, "Found", true);
            context.SetNodeOutput(Id, "X", 0.0);
            context.SetNodeOutput(Id, "Y", 0.0);
            context.SetNodeOutput(Id, "Z", 0.0);
            context.SetNodeOutput(Id, "AxisCount", 3);
            return Task.FromResult(FlowResult.Ok());
        }
    }
}
