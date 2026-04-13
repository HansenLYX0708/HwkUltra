using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.IO.Real
{
    /// <summary>
    /// Digital output node - controls DO signals
    /// </summary>
    public class DigitalOutputNode : DeviceNodeBase<object>  // TODO: Replace with IIoService
    {
        public override string Name { get; set; } = "Digital Output";
        public override string NodeType => "DigitalOutput";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Port", DisplayName = "Port", Type = "int", Required = true, Description = "DO port number" },
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool", Required = true, DefaultValue = true, Description = "true=ON, false=OFF" },
            new FlowParameter { Name = "Duration", DisplayName = "Duration", Type = "int", Required = false, DefaultValue = 0, Description = "Pulse duration in ms (0 = permanent)" },
            new FlowParameter { Name = "WaitForComplete", DisplayName = "Wait For Complete", Type = "bool", Required = false, DefaultValue = true, Description = "Wait for operation to complete" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualValue", DisplayName = "Actual Value", Type = "bool", Description = "Actual output value" },
            new FlowParameter { Name = "OperationTime", DisplayName = "Operation Time", Type = "datetime", Description = "When operation completed" }
        };

        public DigitalOutputNode(object? ioService = null) : base(ioService) { }

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var port = context.GetVariable<int>("Port");
                var value = context.GetVariable<bool>("Value");
                var duration = context.GetVariable<int>("Duration");
                var waitForComplete = context.GetVariable<bool>("WaitForComplete");

                if (IsSimulated)
                {
                    Console.WriteLine($"[DigitalOutput] Simulating DO{port} = {(value ? "ON" : "OFF")}");

                    if (duration > 0)
                    {
                        await Task.Delay(duration, context.CancellationToken);
                        Console.WriteLine($"[DigitalOutput] Simulating DO{port} auto-reset to OFF");
                    }

                    context.SetVariable("ActualValue", value);
                    context.SetVariable("OperationTime", DateTime.Now);
                    return FlowResult.Ok();
                }

                var validationError = ValidateService();
                if (validationError != null) return validationError;

                // TODO: Actual DO operation
                // Service!.SetDigitalOutput(port, value);

                if (duration > 0 && waitForComplete)
                {
                    await Task.Delay(duration, context.CancellationToken);
                    // Service!.SetDigitalOutput(port, false);
                }

                context.SetVariable("ActualValue", value);
                context.SetVariable("OperationTime", DateTime.Now);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Digital output operation failed: {ex.Message}");
            }
        }
    }
}
