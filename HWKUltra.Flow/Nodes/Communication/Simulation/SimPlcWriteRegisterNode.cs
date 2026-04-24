using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimPlcWriteRegisterNode : LogicNodeBase
    {
        public override string Name { get; set; } = "PLC Write Register (Sim)";
        public override string NodeType => "PlcWriteRegister";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "RegisterName", DisplayName = "Register Name", Type = "string", Required = true },
            new FlowParameter { Name = "Value",        DisplayName = "Value",         Type = "int",    Required = true, DefaultValue = 0 }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "RegisterName") ?? "Unknown";
            var value = context.GetNodeInput<int>(Id, "Value");
            Console.WriteLine($"[SIMULATION] PlcWriteRegister: {name} = {value}");
            await Task.Delay(10, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
