using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.TeachData.Simulation
{
    /// <summary>
    /// Simulated MoveToTeachPosition node — no hardware dependency.
    /// </summary>
    public class SimMoveToTeachPositionNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Move To Teach Position (Sim)";
        public override string NodeType => "MoveToTeachPosition";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Required = true, Description = "Teach position name" },
            new FlowParameter { Name = "GroupName", DisplayName = "Motion Group", Type = "string", Required = false, DefaultValue = "XYZ", Description = "Motion axis group name" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 30000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 500000.0, Description = "Acceleration" },
            new FlowParameter { Name = "WaitForComplete", DisplayName = "Wait For Complete", Type = "bool", Required = false, DefaultValue = true, Description = "Wait for motion to complete" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Description = "The position that was moved to" },
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool", Description = "Whether the move completed successfully" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var posName = context.GetNodeInput<string>(Id, "PositionName") ?? "(unknown)";
            Console.WriteLine($"[SIMULATION] MoveToTeachPosition: Moving to '{posName}'");
            await Task.Delay(200, context.CancellationToken);
            context.SetNodeOutput(Id, "PositionName", posName);
            context.SetNodeOutput(Id, "Success", true);
            return FlowResult.Ok();
        }
    }
}
