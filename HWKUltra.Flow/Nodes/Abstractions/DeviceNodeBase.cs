using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Nodes.Abstractions
{
    /// <summary>
    /// Device node base class - handles service injection for all device nodes
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

        protected DeviceNodeBase(TService? service)
        {
            Service = service;
        }

        public abstract Task<FlowResult> ExecuteAsync(FlowContext context);

        /// <summary>
        /// Validate that service is available for real execution
        /// </summary>
        protected FlowResult? ValidateService()
        {
            if (Service == null)
            {
                return FlowResult.Fail($"{NodeType} service not available");
            }
            return null;
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
