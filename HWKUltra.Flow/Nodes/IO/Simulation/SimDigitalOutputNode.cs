using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.IO.Simulation
{
    /// <summary>
    /// Simulated digital output node - no hardware dependency.
    /// </summary>
    public class SimDigitalOutputNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Digital Output (Sim)";
        public override string NodeType => "DigitalOutput";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "OutputName", DisplayName = "Output Name", Type = "string", Required = true, Description = "IO point name (e.g., CameraSwitch, LightTowerRed)" },
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool", Required = true, DefaultValue = true, Description = "true=ON, false=OFF" },
            new FlowParameter { Name = "Duration", DisplayName = "Duration", Type = "int", Required = false, DefaultValue = 0, Description = "Pulse duration in ms (0 = permanent)" },
            new FlowParameter { Name = "WaitForComplete", DisplayName = "Wait For Complete", Type = "bool", Required = false, DefaultValue = true, Description = "Wait for operation to complete" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualValue", DisplayName = "Actual Value", Type = "bool", Description = "Actual output value" },
            new FlowParameter { Name = "OperationTime", DisplayName = "Operation Time", Type = "datetime", Description = "When operation completed" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var outputName = context.GetNodeInput<string>(Id, "OutputName") ?? "Unknown";
            var value = context.GetNodeInput<string>(Id, "Value") != "false";
            var duration = context.GetNodeInput<int>(Id, "Duration");
            Console.WriteLine($"[SIMULATION] DigitalOutput: {outputName} = {(value ? "ON" : "OFF")}");
            if (duration > 0)
                await Task.Delay(duration, context.CancellationToken);
            else
                await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualValue", value);
            context.SetNodeOutput(Id, "OperationTime", DateTime.Now);
            return FlowResult.Ok();
        }
    }
}
