using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimPlcWriteBitNode : LogicNodeBase
    {
        public override string Name { get; set; } = "PLC Write Bit (Sim)";
        public override string NodeType => "PlcWriteBit";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitName", DisplayName = "Bit Name", Type = "string", Required = true },
            new FlowParameter { Name = "Value",   DisplayName = "Value",    Type = "bool",   Required = true, DefaultValue = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "BitName") ?? "Unknown";
            var value = context.GetNodeInput<string>(Id, "Value") ?? "true";
            Console.WriteLine($"[SIMULATION] PlcWriteBit: {name} = {value}");
            await Task.Delay(10, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
