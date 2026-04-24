using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimPlcSendCommandNode : LogicNodeBase
    {
        public override string Name { get; set; } = "PLC Send Command (Sim)";
        public override string NodeType => "PlcSendCommand";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "CommandName", DisplayName = "Command Name", Type = "string", Required = true },
            new FlowParameter { Name = "TimeoutMs",   DisplayName = "Timeout (ms)", Type = "int",    Required = false, DefaultValue = 0 }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool" },
            new FlowParameter { Name = "Message", DisplayName = "Message", Type = "string" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "CommandName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] PlcSendCommand: {name} -> Success");
            await Task.Delay(200, context.CancellationToken);
            context.SetNodeOutput(Id, "Success", true);
            context.SetNodeOutput(Id, "Message", $"Simulated command '{name}' completed");
            return FlowResult.Ok();
        }
    }
}
