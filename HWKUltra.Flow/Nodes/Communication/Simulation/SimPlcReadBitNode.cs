using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimPlcReadBitNode : LogicNodeBase
    {
        public override string Name { get; set; } = "PLC Read Bit (Sim)";
        public override string NodeType => "PlcReadBit";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "BitName",     DisplayName = "Bit Name",      Type = "string", Required = true },
            new FlowParameter { Name = "MockValue",   DisplayName = "Mock Value",    Type = "bool",   Required = false, DefaultValue = false, Description = "Simulated bit value" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "BitName") ?? "Unknown";
            var mockStr = context.GetNodeInput<string>(Id, "MockValue") ?? "false";
            var value = mockStr == "true";
            Console.WriteLine($"[SIMULATION] PlcReadBit: {name} = {value}");
            await Task.Delay(10, context.CancellationToken);
            context.SetNodeOutput(Id, "Value", value);
            return FlowResult.Ok();
        }
    }
}
