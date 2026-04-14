using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulation
{
    /// <summary>
    /// Simulated multi-axis interpolation motion node - no hardware dependency.
    /// </summary>
    public class SimGroupInterpolationNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Group Interpolation (Sim)";
        public override string NodeType => "GroupInterpolation";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "GroupName", DisplayName = "Group Name", Type = "string", Required = true, Description = "e.g., XY, XYZ" },
            new FlowParameter { Name = "X", DisplayName = "X Position", Type = "double", Required = false, Description = "X axis target position" },
            new FlowParameter { Name = "Y", DisplayName = "Y Position", Type = "double", Required = false, Description = "Y axis target position" },
            new FlowParameter { Name = "Z", DisplayName = "Z Position", Type = "double", Required = false, Description = "Z axis target position" },
            new FlowParameter { Name = "Velocity", DisplayName = "Velocity", Type = "double", Required = false, DefaultValue = 30000.0, Description = "Motion velocity" },
            new FlowParameter { Name = "Acceleration", DisplayName = "Acceleration", Type = "double", Required = false, DefaultValue = 500000.0, Description = "Acceleration" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "CompletionStatus", DisplayName = "Completion Status", Type = "bool", Description = "Whether motion completed successfully" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var groupName = context.GetNodeInput<string>(Id, "GroupName") ?? "XY";
            Console.WriteLine($"[SIMULATION] GroupInterpolation: Moving group {groupName}");
            await Task.Delay(200, context.CancellationToken);
            context.SetNodeOutput(Id, "CompletionStatus", true);
            return FlowResult.Ok();
        }
    }
}
