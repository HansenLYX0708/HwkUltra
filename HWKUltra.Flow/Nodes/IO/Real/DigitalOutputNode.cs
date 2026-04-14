using HWKUltra.DeviceIO.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.IO.Real
{
    /// <summary>
    /// Digital output node - controls DO signals via IORouter.
    /// Uses string-based IO point names (e.g., "CameraSwitch", "LightTowerRed").
    /// </summary>
    public class DigitalOutputNode : DeviceNodeBase<IORouter>
    {
        public override string Name { get; set; } = "Digital Output";
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

        protected override int SimulatedDelayMs => 50;

        public DigitalOutputNode(IORouter? ioRouter = null) : base(ioRouter) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var outputName = context.GetNodeInput<string>(Id, "OutputName") ?? "";
                var value = context.GetNodeInput<string>(Id, "Value") != "false";
                var duration = context.GetNodeInput<int>(Id, "Duration");

                if (string.IsNullOrEmpty(outputName))
                    return FlowResult.Fail("OutputName is required");

                if (!Service!.HasOutput(outputName))
                    return FlowResult.Fail($"Output '{outputName}' not found in IO configuration");

                Service.SetOutput(outputName, value);

                if (duration > 0)
                {
                    await Task.Delay(duration, context.CancellationToken);
                    Service.SetOutput(outputName, false);
                }

                context.SetNodeOutput(Id, "ActualValue", value);
                context.SetNodeOutput(Id, "OperationTime", DateTime.Now);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Digital output operation failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var outputName = context.GetNodeInput<string>(Id, "OutputName") ?? "Unknown";
            var value = context.GetNodeInput<string>(Id, "Value") != "false";
            Console.WriteLine($"[SIMULATION] DigitalOutput: {outputName} = {(value ? "ON" : "OFF")}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ActualValue", value);
            context.SetNodeOutput(Id, "OperationTime", DateTime.Now);
            return FlowResult.Ok();
        }
    }
}
