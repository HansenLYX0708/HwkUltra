using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.IO.Simulation
{
    /// <summary>
    /// Simulated digital input node - no hardware dependency.
    /// In simulation mode, inputs always return false.
    /// </summary>
    public class SimDigitalInputNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Digital Input (Sim)";
        public override string NodeType => "DigitalInput";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InputName", DisplayName = "Input Name", Type = "string", Required = true, Description = "IO point name (e.g., EMO, DoorSensor)" },
            new FlowParameter { Name = "WaitForTrue", DisplayName = "Wait For True", Type = "bool", Required = false, DefaultValue = false, Description = "Wait until input becomes true" },
            new FlowParameter { Name = "Timeout", DisplayName = "Timeout", Type = "int", Required = false, DefaultValue = 5000, Description = "Wait timeout in ms (0 = no timeout)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool", Description = "Input value" },
            new FlowParameter { Name = "ReadTime", DisplayName = "Read Time", Type = "datetime", Description = "When input was read" },
            new FlowParameter { Name = "TimedOut", DisplayName = "Timed Out", Type = "bool", Description = "True if wait timed out" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var inputName = context.GetNodeInput<string>(Id, "InputName") ?? "Unknown";
            var waitForTrue = context.GetNodeInput<string>(Id, "WaitForTrue") != "false";

            // In simulation, inputs are always false, so WaitForTrue would timeout
            bool value = false;
            bool timedOut = waitForTrue;

            Console.WriteLine($"[SIMULATION] DigitalInput: {inputName} = {value}");
            await Task.Delay(10, context.CancellationToken);

            context.SetNodeOutput(Id, "Value", value);
            context.SetNodeOutput(Id, "ReadTime", DateTime.Now);
            context.SetNodeOutput(Id, "TimedOut", timedOut);

            return FlowResult.Ok();
        }
    }
}
