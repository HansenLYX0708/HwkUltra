namespace HWKUltra.Flow.Nodes.Abstractions
{
    /// <summary>
    /// Interface for nodes that support simulation mode
    /// </summary>
    public interface ISimulatedNode
    {
        /// <summary>
        /// Whether to simulate execution (no actual hardware operation)
        /// </summary>
        bool SimulateExecution { get; set; }

        /// <summary>
        /// Simulated execution delay in milliseconds
        /// </summary>
        int SimulatedDelayMs { get; set; }

        /// <summary>
        /// Log simulation activity
        /// </summary>
        void LogSimulation(string activity);
    }

    /// <summary>
    /// Extension methods for simulated nodes
    /// </summary>
    public static class SimulatedNodeExtensions
    {
        /// <summary>
        /// Simulate a delay and log the activity
        /// </summary>
        public static async Task SimulateDelayAsync(this ISimulatedNode node, CancellationToken cancellationToken = default)
        {
            if (node.SimulatedDelayMs > 0)
            {
                await Task.Delay(node.SimulatedDelayMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Default implementation of ISimulatedNode that can be used as a mixin
    /// </summary>
    public class SimulationSettings : ISimulatedNode
    {
        public bool SimulateExecution { get; set; } = true;
        public int SimulatedDelayMs { get; set; } = 100;
        public string NodeName { get; set; } = string.Empty;

        public void LogSimulation(string activity)
        {
            Console.WriteLine($"[SIMULATION] {NodeName}: {activity}");
        }
    }
}
