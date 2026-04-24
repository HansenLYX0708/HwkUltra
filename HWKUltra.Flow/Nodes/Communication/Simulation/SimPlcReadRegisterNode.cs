using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Communication.Simulation
{
    public class SimPlcReadRegisterNode : LogicNodeBase
    {
        public override string Name { get; set; } = "PLC Read Register (Sim)";
        public override string NodeType => "PlcReadRegister";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "RegisterName", DisplayName = "Register Name", Type = "string", Required = true },
            new FlowParameter { Name = "MockValue",    DisplayName = "Mock Value",    Type = "int",    Required = false, DefaultValue = 0 }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "int" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "RegisterName") ?? "Unknown";
            var mockValue = context.GetNodeInput<int>(Id, "MockValue");
            Console.WriteLine($"[SIMULATION] PlcReadRegister: {name} = {mockValue}");
            await Task.Delay(10, context.CancellationToken);
            context.SetNodeOutput(Id, "Value", mockValue);
            return FlowResult.Ok();
        }
    }
}
