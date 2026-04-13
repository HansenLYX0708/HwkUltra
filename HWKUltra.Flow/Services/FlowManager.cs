using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using System.Collections.Concurrent;

namespace HWKUltra.Flow.Services
{
    /// <summary>
    /// Flow manager - manages flow definitions and execution instances
    /// </summary>
    public class FlowManager
    {
        private readonly ConcurrentDictionary<string, FlowDefinition> _definitions = new();
        private readonly ConcurrentDictionary<Guid, FlowInstance> _runningInstances = new();
        private readonly IFlowNodeFactory _nodeFactory;

        public FlowManager(IFlowNodeFactory nodeFactory)
        {
            _nodeFactory = nodeFactory;
        }

        /// <summary>
        /// Create new flow definition
        /// </summary>
        public FlowDefinition CreateDefinition(string name, string? description = null)
        {
            var definition = new FlowDefinition
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            _definitions[definition.Id] = definition;
            return definition;
        }

        /// <summary>
        /// Get flow definition
        /// </summary>
        public FlowDefinition? GetDefinition(string id)
        {
            _definitions.TryGetValue(id, out var definition);
            return definition;
        }

        /// <summary>
        /// Get all flow definitions
        /// </summary>
        public IEnumerable<FlowDefinition> GetAllDefinitions()
        {
            return _definitions.Values;
        }

        /// <summary>
        /// Update flow definition
        /// </summary>
        public void UpdateDefinition(FlowDefinition definition)
        {
            definition.ModifiedAt = DateTime.Now;
            _definitions[definition.Id] = definition;
        }

        /// <summary>
        /// Delete flow definition
        /// </summary>
        public bool DeleteDefinition(string id)
        {
            return _definitions.TryRemove(id, out _);
        }

        /// <summary>
        /// Execute flow with defensive copy to prevent modification during execution
        /// </summary>
        public async Task<FlowResult> ExecuteAsync(string definitionId, FlowContext? context = null, CancellationToken cancellationToken = default)
        {
            var definition = GetDefinition(definitionId);
            if (definition == null)
                throw new ArgumentException($"Flow definition not found: {definitionId}");

            // Create defensive copy to prevent external modification during execution
            var executionDef = new FlowDefinition
            {
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description,
                CreatedAt = definition.CreatedAt,
                ModifiedAt = definition.ModifiedAt,
                Version = definition.Version,
                StartNodeId = definition.StartNodeId,
                Nodes = definition.Nodes.ToList(),
                Connections = definition.Connections.ToList(),
                GlobalVariables = definition.GlobalVariables.ToList()
            };

            // Create flow engine with defensive copy
            var engine = new FlowEngine(executionDef);

            // Register all nodes from defensive copy
            foreach (var nodeDef in executionDef.Nodes)
            {
                var node = _nodeFactory.CreateNode(nodeDef.Type, nodeDef.Properties);
                node.Id = nodeDef.Id;
                node.Name = nodeDef.Name;
                node.Description = nodeDef.Description;
                engine.RegisterNode(node);
            }

            // Create flow context, set node properties as default values
            context ??= new FlowContext();
            foreach (var nodeDef in executionDef.Nodes)
            {
                foreach (var prop in nodeDef.Properties)
                {
                    // Only set undefined variables
                    if (!context.Variables.ContainsKey(prop.Key))
                    {
                        context.SetVariable(prop.Key, prop.Value);
                    }
                }
            }

            // Create flow instance
            var instance = new FlowInstance
            {
                DefinitionId = definitionId,
                Engine = engine,
                StartTime = DateTime.UtcNow
            };

            _runningInstances[instance.Id] = instance;

            // Subscribe to events
            engine.NodeExecuting += (s, e) => OnNodeExecuting(instance, e);
            engine.NodeExecuted += (s, e) => OnNodeExecuted(instance, e);
            engine.FlowError += (s, e) => OnFlowError(instance, e);
            engine.FlowCompleted += (s, e) => OnFlowCompleted(instance);

            try
            {
                var result = await engine.ExecuteAsync(context, cancellationToken);
                instance.Result = result;
                return result;
            }
            finally
            {
                instance.EndTime = DateTime.UtcNow;
                _runningInstances.TryRemove(instance.Id, out _);
            }
        }

        /// <summary>
        /// Get running flow instances
        /// </summary>
        public IEnumerable<FlowInstance> GetRunningInstances()
        {
            return _runningInstances.Values;
        }

        #region Event Handlers

        private void OnNodeExecuting(FlowInstance instance, FlowNodeEventArgs e)
        {
            NodeExecuting?.Invoke(this, new FlowInstanceEventArgs(instance, e.Node, e.Context));
        }

        private void OnNodeExecuted(FlowInstance instance, FlowNodeEventArgs e)
        {
            NodeExecuted?.Invoke(this, new FlowInstanceEventArgs(instance, e.Node, e.Context, e.Result));
        }

        private void OnFlowError(FlowInstance instance, FlowErrorEventArgs e)
        {
            FlowError?.Invoke(this, new FlowInstanceErrorEventArgs(instance, e.Node, e.ErrorMessage, e.Context));
        }

        private void OnFlowCompleted(FlowInstance instance)
        {
            FlowCompleted?.Invoke(this, new FlowInstanceCompletedEventArgs(instance));
        }

        #endregion

        #region Events

        public event EventHandler<FlowInstanceEventArgs>? NodeExecuting;
        public event EventHandler<FlowInstanceEventArgs>? NodeExecuted;
        public event EventHandler<FlowInstanceErrorEventArgs>? FlowError;
        public event EventHandler<FlowInstanceCompletedEventArgs>? FlowCompleted;

        #endregion
    }

    /// <summary>
    /// Flow instance
    /// </summary>
    public class FlowInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string DefinitionId { get; set; } = string.Empty;
        public FlowEngine Engine { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public FlowResult? Result { get; set; }

        public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : null;
    }

    /// <summary>
    /// Node factory interface
    /// </summary>
    public interface IFlowNodeFactory
    {
        IFlowNode CreateNode(string type, Dictionary<string, string> properties);
    }

    #region Event Arguments

    public class FlowInstanceEventArgs : EventArgs
    {
        public FlowInstance Instance { get; }
        public IFlowNode Node { get; }
        public FlowContext Context { get; }
        public FlowResult? Result { get; }

        public FlowInstanceEventArgs(FlowInstance instance, IFlowNode node, FlowContext context, FlowResult? result = null)
        {
            Instance = instance;
            Node = node;
            Context = context;
            Result = result;
        }
    }

    public class FlowInstanceErrorEventArgs : EventArgs
    {
        public FlowInstance Instance { get; }
        public IFlowNode? Node { get; }
        public string ErrorMessage { get; }
        public FlowContext Context { get; }

        public FlowInstanceErrorEventArgs(FlowInstance instance, IFlowNode? node, string errorMessage, FlowContext context)
        {
            Instance = instance;
            Node = node;
            ErrorMessage = errorMessage;
            Context = context;
        }
    }

    public class FlowInstanceCompletedEventArgs : EventArgs
    {
        public FlowInstance Instance { get; }

        public FlowInstanceCompletedEventArgs(FlowInstance instance)
        {
            Instance = instance;
        }
    }

    #endregion
}
