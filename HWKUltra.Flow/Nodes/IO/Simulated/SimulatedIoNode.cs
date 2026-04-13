using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.IO.Simulated
{
    /// <summary>
    /// Simulated IO node - for testing without hardware
    /// </summary>
    public class SimulatedIoNode : LogicNodeBase, ISimulatedNode
    {
        public override string Name { get; set; } = "Simulated IO";
        public override string NodeType => "IoOutput";

        public bool SimulateExecution { get; set; } = true;
        public int SimulatedDelayMs { get; set; } = 50;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Port", DisplayName = "Port", Type = "int", Required = true, Description = "IO port number" },
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool", Required = true, DefaultValue = true, Description = "true=ON, false=OFF" },
            new FlowParameter { Name = "Duration", DisplayName = "Duration", Type = "int", Required = false, DefaultValue = 0, Description = "Pulse duration in ms" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ActualValue", DisplayName = "Actual Value", Type = "bool", Description = "Simulated output value" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var port = context.GetVariable<int>("Port");
                var value = context.GetVariable<bool>("Value");
                var duration = context.GetVariable<int>("Duration");

                LogSimulation($"Setting port {port} = {(value ? "ON" : "OFF")}");

                if (duration > 0)
                {
                    await Task.Delay(duration, context.CancellationToken);
                    LogSimulation($"Auto-reset port {port} to OFF after {duration}ms");
                    context.SetVariable("ActualValue", false);
                }
                else
                {
                    context.SetVariable("ActualValue", value);
                }

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Simulated IO operation failed: {ex.Message}");
            }
        }

        public void LogSimulation(string activity)
        {
            Console.WriteLine($"[SIMULATION] {Name}: {activity}");
        }
    }
}
