using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Motion.Simulated
{
    /// <summary>
    /// Simulated axis homing node - for testing without hardware
    /// </summary>
    public class SimulatedAxisHomeNode : LogicNodeBase, ISimulatedNode
    {
        public override string Name { get; set; } = "Simulated Axis Home";
        public override string NodeType => "AxisHome";

        public bool SimulateExecution { get; set; } = true;
        public int SimulatedDelayMs { get; set; } = 500;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, Description = "e.g., X, Y, Z" },
            new FlowParameter { Name = "HomeMode", DisplayName = "Home Mode", Type = "string", Required = false, DefaultValue = "Auto", Description = "Auto, Positive, Negative" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "HomeCompleted", DisplayName = "Home Completed", Type = "bool" },
            new FlowParameter { Name = "HomePosition", DisplayName = "Home Position", Type = "double" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var axisName = context.GetVariable<string>("AxisName") ?? "X";
                var homeMode = context.GetVariable<string>("HomeMode") ?? "Auto";

                LogSimulation($"Homing axis {axisName}, mode={homeMode}");
                await Task.Delay(SimulatedDelayMs, context.CancellationToken);

                context.SetVariable("HomeCompleted", true);
                context.SetVariable("HomePosition", 0.0);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Simulated homing failed: {ex.Message}");
            }
        }

        public void LogSimulation(string activity)
        {
            Console.WriteLine($"[SIMULATION] {Name}: {activity}");
        }
    }
}
