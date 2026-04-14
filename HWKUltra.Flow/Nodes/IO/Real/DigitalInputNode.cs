using HWKUltra.DeviceIO.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.IO.Real
{
    /// <summary>
    /// Digital input node - reads DI signals via IORouter.
    /// Uses string-based IO point names (e.g., "EMO", "DoorSensor").
    /// </summary>
    public class DigitalInputNode : DeviceNodeBase<IORouter>
    {
        public override string Name { get; set; } = "Digital Input";
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

        protected override int SimulatedDelayMs => 10;

        public DigitalInputNode(IORouter? ioRouter = null) : base(ioRouter) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            try
            {
                var inputName = context.GetNodeInput<string>(Id, "InputName") ?? "";
                var waitForTrue = context.GetNodeInput<string>(Id, "WaitForTrue") != "false";
                var timeout = context.GetNodeInput<int>(Id, "Timeout");

                if (string.IsNullOrEmpty(inputName))
                    return FlowResult.Fail("InputName is required");

                if (!Service!.HasInput(inputName))
                    return FlowResult.Fail($"Input '{inputName}' not found in IO configuration");

                bool value;
                bool timedOut = false;

                if (waitForTrue)
                {
                    // Poll until input becomes true or timeout
                    var startTime = DateTime.UtcNow;
                    var timeoutSpan = TimeSpan.FromMilliseconds(timeout > 0 ? timeout : int.MaxValue);

                    while (true)
                    {
                        value = Service.GetInput(inputName);
                        if (value)
                            break;

                        if (timeout > 0 && (DateTime.UtcNow - startTime) > timeoutSpan)
                        {
                            timedOut = true;
                            break;
                        }

                        await Task.Delay(10, context.CancellationToken);
                    }
                }
                else
                {
                    value = Service.GetInput(inputName);
                }

                context.SetNodeOutput(Id, "Value", value);
                context.SetNodeOutput(Id, "ReadTime", DateTime.Now);
                context.SetNodeOutput(Id, "TimedOut", timedOut);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Digital input operation failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var inputName = context.GetNodeInput<string>(Id, "InputName") ?? "Unknown";
            var waitForTrue = context.GetNodeInput<string>(Id, "WaitForTrue") != "false";

            // In simulation, inputs are always false, so WaitForTrue would timeout
            bool value = false;
            bool timedOut = waitForTrue;

            Console.WriteLine($"[SIMULATION] DigitalInput: {inputName} = {value}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);

            context.SetNodeOutput(Id, "Value", value);
            context.SetNodeOutput(Id, "ReadTime", DateTime.Now);
            context.SetNodeOutput(Id, "TimedOut", timedOut);

            return FlowResult.Ok();
        }
    }
}
