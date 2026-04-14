using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Nodes.Abstractions
{
    /// <summary>
    /// Device node base class - handles service injection and simulation fallback
    /// When Service is null, automatically runs in simulation mode (log + delay)
    /// </summary>
    public abstract class DeviceNodeBase<TService> : IFlowNode
        where TService : class
    {
        protected readonly TService? Service;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public abstract string Name { get; set; }
        public abstract string NodeType { get; }
        public string? Description { get; set; }
        public abstract List<FlowParameter> Inputs { get; }
        public abstract List<FlowParameter> Outputs { get; }

        /// <summary>
        /// Whether this node is running in simulation mode (no real hardware)
        /// </summary>
        public bool IsSimulated => Service == null;

        /// <summary>
        /// Simulated delay in ms (subclass can override)
        /// </summary>
        protected virtual int SimulatedDelayMs => 100;

        protected DeviceNodeBase(TService? service)
        {
            Service = service;
        }

        public async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            if (IsSimulated)
                return await ExecuteSimulatedAsync(context);

            return await ExecuteRealAsync(context);
        }

        /// <summary>
        /// Real hardware execution - subclass implements actual device control
        /// </summary>
        protected abstract Task<FlowResult> ExecuteRealAsync(FlowContext context);

        /// <summary>
        /// Default simulation: log + delay. Subclass can override for custom simulation.
        /// </summary>
        protected virtual async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            Console.WriteLine($"[SIMULATION] {NodeType}({Name}): simulated execution");
            if (SimulatedDelayMs > 0)
                await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }

    /// <summary>
    /// Logic node base class - for nodes that don't require hardware services
    /// </summary>
    public abstract class LogicNodeBase : IFlowNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public abstract string Name { get; set; }
        public abstract string NodeType { get; }
        public string? Description { get; set; }
        public abstract List<FlowParameter> Inputs { get; }
        public abstract List<FlowParameter> Outputs { get; }

        public abstract Task<FlowResult> ExecuteAsync(FlowContext context);
    }

    /// <summary>
    /// Composite node base class - for advanced features combining multiple devices
    /// </summary>
    public abstract class CompositeNodeBase : IFlowNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public abstract string Name { get; set; }
        public abstract string NodeType { get; }
        public string? Description { get; set; }
        public abstract List<FlowParameter> Inputs { get; }
        public abstract List<FlowParameter> Outputs { get; }

        /// <summary>
        /// Whether running in simulation mode
        /// </summary>
        public bool IsSimulated { get; protected set; }

        protected CompositeNodeBase(bool simulate = false)
        {
            IsSimulated = simulate;
        }

        public abstract Task<FlowResult> ExecuteAsync(FlowContext context);
    }
}
