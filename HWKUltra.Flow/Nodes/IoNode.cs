using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Nodes
{
    /// <summary>
    /// IO control node - controls input/output signals
    /// </summary>
    public class IoOutputNode : IFlowNode
    {
        // TODO: Inject IO service
        // private readonly IIoService _ioService;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "IO Output";
        public string NodeType => "IoOutput";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Port", DisplayName = "Port", Type = "int", Required = true, Description = "IO port number" },
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool", Required = true, DefaultValue = true, Description = "true=High, false=Low" },
            new FlowParameter { Name = "Duration", DisplayName = "Duration", Type = "int", Required = false, DefaultValue = 0, Description = "ms, 0=Permanent output" }
        };

        public List<FlowParameter> Outputs { get; } = new();

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var port = context.GetVariable<int>("Port");
                var value = context.GetVariable<bool>("Value");
                var duration = context.GetVariable<int>("Duration");

                Console.WriteLine($"[IO] Setting port {port} = {(value ? "ON" : "OFF")}");

                // TODO: Actually call IO service
                // _ioService.SetOutput(port, value);

                if (duration > 0)
                {
                    await Task.Delay(duration, context.CancellationToken);
                    Console.WriteLine($"[IO] Port {port} reset to OFF");
                    // _ioService.SetOutput(port, false);
                }

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"IO operation failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// IO input detection node
    /// </summary>
    public class IoInputNode : IFlowNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "IO Input Detection";
        public string NodeType => "IoInput";
        public string? Description { get; set; }

        public List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Port", DisplayName = "Port", Type = "int", Required = true },
            new FlowParameter { Name = "ExpectedValue", DisplayName = "Expected Value", Type = "bool", Required = true, DefaultValue = true },
            new FlowParameter { Name = "Timeout", DisplayName = "Timeout", Type = "int", Required = false, DefaultValue = 5000, Description = "Milliseconds" }
        };

        public List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualValue", DisplayName = "Actual Value", Type = "bool" }
        };

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var port = context.GetVariable<int>("Port");
                var expectedValue = context.GetVariable<bool>("ExpectedValue");
                var timeout = context.GetVariable<int>("Timeout");

                Console.WriteLine($"[IO] Waiting for port {port} = {(expectedValue ? "ON" : "OFF")}, timeout {timeout}ms");

                // TODO: Actually poll IO status
                // var actualValue = await _ioService.WaitForInputAsync(port, expectedValue, timeout);

                await Task.Delay(100, context.CancellationToken);
                var actualValue = true;

                context.SetVariable("ActualValue", actualValue);

                if (actualValue != expectedValue)
                {
                    return FlowResult.Fail($"IO input detection timeout: port {port}");
                }

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"IO input detection failed: {ex.Message}");
            }
        }
    }
}
