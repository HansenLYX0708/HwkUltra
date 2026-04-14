using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Simulation
{
    public class SimTrayGetInfoNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Tray GetInfo (Sim)";
        public override string NodeType => "TrayGetInfo";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "TotalSlots", DisplayName = "Total Slots", Type = "int" },
            new FlowParameter { Name = "TestedCount", DisplayName = "Tested", Type = "int" },
            new FlowParameter { Name = "PassCount", DisplayName = "Pass", Type = "int" },
            new FlowParameter { Name = "FailCount", DisplayName = "Fail", Type = "int" },
            new FlowParameter { Name = "ErrorCount", DisplayName = "Error", Type = "int" },
            new FlowParameter { Name = "TestState", DisplayName = "TestState", Type = "int" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayGetInfo: {name}");
            await Task.Delay(10, context.CancellationToken);
            context.SetNodeOutput(Id, "TotalSlots", 240);
            context.SetNodeOutput(Id, "TestedCount", 0);
            context.SetNodeOutput(Id, "PassCount", 0);
            context.SetNodeOutput(Id, "FailCount", 0);
            context.SetNodeOutput(Id, "ErrorCount", 0);
            context.SetNodeOutput(Id, "TestState", 0);
            return FlowResult.Ok();
        }
    }
}
