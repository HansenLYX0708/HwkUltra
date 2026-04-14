using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Motion.Real;
using HWKUltra.Flow.Nodes.Camera.Real;
using HWKUltra.Flow.Nodes.Laser.Real;
using HWKUltra.Flow.Nodes.IO.Real;
using HWKUltra.Flow.Nodes.Logic;
using HWKUltra.Flow.Nodes.Advanced.Real;
using HWKUltra.Motion.Core;
using HWKUltra.DeviceIO.Core;

namespace HWKUltra.Flow.Services
{
    /// <summary>
    /// Default node factory - creates nodes with service injection.
    /// Simulation is handled automatically by DeviceNodeBase when service is null.
    /// </summary>
    public class DefaultNodeFactory : IFlowNodeFactory
    {
        private readonly MotionRouter? _motionRouter;
        private readonly IORouter? _ioRouter;
        private readonly object? _cameraService;  // TODO: Replace with ICameraService
        private readonly object? _laserService;   // TODO: Replace with ILaserService

        public bool UseSimulation { get; set; } = false;

        public DefaultNodeFactory(
            MotionRouter? motionRouter = null,
            IORouter? ioRouter = null,
            object? cameraService = null,
            object? laserService = null)
        {
            _motionRouter = motionRouter;
            _ioRouter = ioRouter;
            _cameraService = cameraService;
            _laserService = laserService;
        }

        public IFlowNode CreateNode(string type, Dictionary<string, string> properties)
        {
            var node = CreateNodeInternal(type);

            // Restore node general configuration from properties
            if (properties.TryGetValue("Name", out var name) && !string.IsNullOrEmpty(name))
                node.Name = name;
            if (properties.TryGetValue("Description", out var description))
                node.Description = description;

            return node;
        }

        /// <summary>
        /// Create node with explicit simulation mode (for backward compatibility)
        /// </summary>
        public IFlowNode CreateNode(string type, Dictionary<string, string> properties, bool useSimulation)
        {
            // When useSimulation is true, inject null services to force simulation in DeviceNodeBase
            var node = useSimulation ? CreateNodeSimulated(type) : CreateNodeInternal(type);

            if (properties.TryGetValue("Name", out var name) && !string.IsNullOrEmpty(name))
                node.Name = name;
            if (properties.TryGetValue("Description", out var description))
                node.Description = description;

            return node;
        }

        private IFlowNode CreateNodeInternal(string type)
        {
            return type switch
            {
                // Motion Control - uses MotionRouter (null → auto simulation)
                "AxisHome" => new AxisHomeNode(_motionRouter),
                "AxisMoveAbs" => new AxisMoveAbsNode(_motionRouter),
                "AxisMoveRel" => new AxisMoveRelNode(_motionRouter),
                "AxisMoveVelocity" => new AxisMoveVelocityNode(_motionRouter),
                "AxisWaitInPos" => new AxisWaitInPosNode(_motionRouter),
                "GroupInterpolation" => new GroupInterpolationNode(_motionRouter),

                // Camera (null → auto simulation)
                "CameraTrigger" or "Camera" => new CameraTriggerNode(_cameraService),

                // Laser (null → auto simulation)
                "LaserTrigger" or "Laser" => new LaserTriggerNode(_laserService),

                // IO (null → auto simulation)
                "DigitalOutput" or "IoOutput" => new DigitalOutputNode(_ioRouter),
                "DigitalInput" or "IoInput" => new DigitalInputNode(_ioRouter),

                // Logic Control - no hardware dependency
                "Delay" => new DelayNode(),
                "Branch" => new BranchNode(),
                "Loop" => new LoopNode(),

                // Advanced Features
                "OnTheFlyCapture" => new OnTheFlyCaptureNode(_motionRouter, _cameraService),

                // Legacy compatibility
                "Motion" => new AxisMoveAbsNode(_motionRouter) { Name = "Motion (Legacy)" },
                "MotionGroup" => new GroupInterpolationNode(_motionRouter) { Name = "MotionGroup (Legacy)" },
                "WaitForAxis" => new AxisWaitInPosNode(_motionRouter) { Name = "WaitForAxis (Legacy)" },

                // Default
                _ => throw new ArgumentException($"Unknown node type: {type}")
            };
        }

        /// <summary>
        /// Create node with null services to force simulation mode
        /// </summary>
        private IFlowNode CreateNodeSimulated(string type)
        {
            return type switch
            {
                "AxisHome" => new AxisHomeNode(null),
                "AxisMoveAbs" => new AxisMoveAbsNode(null),
                "AxisMoveRel" => new AxisMoveRelNode(null),
                "AxisMoveVelocity" => new AxisMoveVelocityNode(null),
                "AxisWaitInPos" => new AxisWaitInPosNode(null),
                "GroupInterpolation" => new GroupInterpolationNode(null),
                "CameraTrigger" or "Camera" => new CameraTriggerNode(null),
                "LaserTrigger" or "Laser" => new LaserTriggerNode(null),
                "DigitalOutput" or "IoOutput" => new DigitalOutputNode(null),
                "DigitalInput" or "IoInput" => new DigitalInputNode(null),
                "Delay" => new DelayNode(),
                "Branch" => new BranchNode(),
                "Loop" => new LoopNode(),
                "OnTheFlyCapture" => new OnTheFlyCaptureNode(null, null, true),
                "Motion" => new AxisMoveAbsNode(null) { Name = "Motion (Legacy)" },
                "MotionGroup" => new GroupInterpolationNode(null) { Name = "MotionGroup (Legacy)" },
                "WaitForAxis" => new AxisWaitInPosNode(null) { Name = "WaitForAxis (Legacy)" },
                _ => throw new ArgumentException($"Unknown node type: {type}")
            };
        }
    }
}
