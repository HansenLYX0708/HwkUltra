using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.TeachData.Simulation
{
    /// <summary>
    /// Simulated SetTeachPosition node — logs only, no actual persistence.
    /// </summary>
    public class SimSetTeachPositionNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Set Teach Position (Sim)";
        public override string NodeType => "SetTeachPosition";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Required = true, Description = "Teach position name" },
            new FlowParameter { Name = "Group", DisplayName = "Group", Type = "string", Required = false, DefaultValue = "Custom", Description = "Group name" },
            new FlowParameter { Name = "X", DisplayName = "X", Type = "double", Required = false, DefaultValue = 0.0, Description = "X axis value" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "double", Required = false, DefaultValue = 0.0, Description = "Y axis value" },
            new FlowParameter { Name = "Z", DisplayName = "Z", Type = "double", Required = false, DefaultValue = 0.0, Description = "Z axis value" },
            new FlowParameter { Name = "AutoSave", DisplayName = "Auto Save", Type = "bool", Required = false, DefaultValue = false, Description = "Save to file immediately" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool", Description = "Whether the position was set" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var posName = context.GetNodeInput<string>(Id, "PositionName") ?? "(unknown)";
            var x = context.GetNodeInput<double>(Id, "X");
            var y = context.GetNodeInput<double>(Id, "Y");
            var z = context.GetNodeInput<double>(Id, "Z");
            Console.WriteLine($"[SIMULATION] SetTeachPosition: '{posName}' X={x:F3} Y={y:F3} Z={z:F3}");
            context.SetNodeOutput(Id, "Success", true);
            return Task.FromResult(FlowResult.Ok());
        }
    }
}
