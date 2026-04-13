using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes;
using HWKUltra.Flow.Nodes.Motion.Real;
using HWKUltra.Flow.Nodes.Motion.Simulated;
using HWKUltra.Flow.Nodes.Camera.Real;
using HWKUltra.Flow.Nodes.Camera.Simulated;
using HWKUltra.Flow.Nodes.Laser.Real;
using HWKUltra.Flow.Nodes.Laser.Simulated;
using HWKUltra.Flow.Nodes.IO.Real;
using HWKUltra.Flow.Nodes.IO.Simulated;
using HWKUltra.Flow.Nodes.Logic;
using HWKUltra.Flow.Nodes.Advanced.Real;
using HWKUltra.Flow.Nodes.Advanced.Simulated;
using HWKUltra.Motion.Abstractions;

namespace HWKUltra.Flow.Services
{
    /// <summary>
    /// Default node factory implementation - creates nodes based on type and simulation mode
    /// </summary>
    public class DefaultNodeFactory : IFlowNodeFactory
    {
        private readonly IMotionController? _motionController;
        private readonly object? _cameraService;  // TODO: Replace with ICameraService
        private readonly object? _laserService;   // TODO: Replace with ILaserService
        private readonly object? _ioService;      // TODO: Replace with IIoService

        public bool UseSimulation { get; set; } = false;

        public DefaultNodeFactory(
            IMotionController? motionController = null,
            object? cameraService = null,
            object? laserService = null,
            object? ioService = null)
        {
            _motionController = motionController;
            _cameraService = cameraService;
            _laserService = laserService;
            _ioService = ioService;
        }

        public IFlowNode CreateNode(string type, Dictionary<string, string> properties)
        {
            var node = CreateNodeInternal(type, UseSimulation);

            // Restore node general configuration from properties
            if (properties.TryGetValue("Name", out var name) && !string.IsNullOrEmpty(name))
                node.Name = name;
            if (properties.TryGetValue("Description", out var description))
                node.Description = description;

            return node;
        }

        /// <summary>
        /// Create node with explicit simulation mode
        /// </summary>
        public IFlowNode CreateNode(string type, Dictionary<string, string> properties, bool useSimulation)
        {
            var node = CreateNodeInternal(type, useSimulation);

            // Restore node general configuration from properties
            if (properties.TryGetValue("Name", out var name) && !string.IsNullOrEmpty(name))
                node.Name = name;
            if (properties.TryGetValue("Description", out var description))
                node.Description = description;

            return node;
        }

        private IFlowNode CreateNodeInternal(string type, bool useSimulation)
        {
            // Legacy node types (backward compatibility)
            var legacyNode = TryCreateLegacyNode(type, useSimulation);
            if (legacyNode != null) return legacyNode;

            // New categorized node types
            return type switch
            {
                // Motion Control - Basic
                "AxisHome" => useSimulation
                    ? new SimulatedAxisHomeNode()
                    : new AxisHomeNode(_motionController),
                "AxisMoveAbs" => useSimulation
                    ? new SimulatedAxisMoveNode { SimulatedDelayMs = 100 }
                    : new AxisMoveAbsNode(_motionController),
                "AxisMoveRel" => useSimulation
                    ? new SimulatedAxisMoveNode { SimulatedDelayMs = 100 }
                    : new AxisMoveRelNode(_motionController),
                "AxisMoveVelocity" => useSimulation
                    ? new SimulatedAxisMoveNode { SimulatedDelayMs = 50 }
                    : new AxisMoveVelocityNode(_motionController),
                "AxisWaitInPos" => useSimulation
                    ? new SimulatedAxisMoveNode { Name = "Simulated Axis Wait", SimulatedDelayMs = 50 }
                    : new AxisWaitInPosNode(_motionController),
                "GroupInterpolation" => useSimulation
                    ? new SimulatedAxisMoveNode { Name = "Simulated Group Move", SimulatedDelayMs = 200 }
                    : new GroupInterpolationNode(_motionController),

                // Camera
                "CameraTrigger" => useSimulation
                    ? new SimulatedCameraNode { SimulatedDelayMs = 50 }
                    : new CameraTriggerNode(_cameraService),
                "Camera" => useSimulation
                    ? new SimulatedCameraNode()
                    : throw new InvalidOperationException("Camera service not available. Use CameraTrigger or enable simulation."),

                // Laser
                "LaserTrigger" => useSimulation
                    ? new SimulatedLaserNode { SimulatedDelayMs = 50 }
                    : new LaserTriggerNode(_laserService),
                "Laser" => useSimulation
                    ? new SimulatedLaserNode()
                    : throw new InvalidOperationException("Laser service not available. Use LaserTrigger or enable simulation."),

                // IO
                "DigitalOutput" => useSimulation
                    ? new SimulatedIoNode { SimulatedDelayMs = 50 }
                    : new DigitalOutputNode(_ioService),
                "IoOutput" => useSimulation
                    ? new SimulatedIoNode()
                    : new DigitalOutputNode(_ioService),

                // Logic Control
                "Delay" => new DelayNode(),
                "Branch" => new BranchNode(),
                "Loop" => new LoopNode(),

                // Advanced Features
                "OnTheFlyCapture" => useSimulation
                    ? new SimulatedOnTheFlyNode()
                    : new OnTheFlyCaptureNode(_motionController, _cameraService, useSimulation),

                // Default
                _ => throw new ArgumentException($"Unknown node type: {type}")
            };
        }

        /// <summary>
        /// Try to create legacy node types for backward compatibility
        /// </summary>
        private IFlowNode? TryCreateLegacyNode(string type, bool useSimulation)
        {
            if (useSimulation)
            {
                // In simulation mode, map legacy types to simulated versions
                return type switch
                {
                    "Motion" => new SimulatedAxisMoveNode { Name = "Motion (Legacy Simulated)" },
                    "MotionGroup" => new SimulatedAxisMoveNode { Name = "MotionGroup (Legacy Simulated)", SimulatedDelayMs = 200 },
                    "WaitForAxis" => new SimulatedAxisMoveNode { Name = "WaitForAxis (Legacy Simulated)", SimulatedDelayMs = 50 },
                    "Camera" => new SimulatedCameraNode(),
                    "Inspection" => new InspectionNode(),  // TODO: Create SimulatedInspectionNode
                    "Laser" => new SimulatedLaserNode(),
                    "IoOutput" => new SimulatedIoNode(),
                    "IoInput" => new SimulatedIoNode { Name = "IO Input (Simulated)" },
                    "Delay" => new DelayNode(),
                    _ => null
                };
            }

            // Real hardware mode - legacy types mapped to new implementations
            return type switch
            {
                "Motion" => _motionController != null
                    ? new AxisMoveAbsNode(_motionController) { Name = "Motion (Legacy)" }
                    : new SimulatedAxisMoveNode { Name = "Motion (Fallback Simulated)" },
                "MotionGroup" => _motionController != null
                    ? new GroupInterpolationNode(_motionController) { Name = "MotionGroup (Legacy)" }
                    : new SimulatedAxisMoveNode { Name = "MotionGroup (Fallback Simulated)", SimulatedDelayMs = 200 },
                "WaitForAxis" => new AxisWaitInPosNode(_motionController) { Name = "WaitForAxis (Legacy)" },
                "Camera" => new SimulatedCameraNode { Name = "Camera (Legacy Simulated)" },
                "Inspection" => new InspectionNode(),
                "Laser" => new SimulatedLaserNode { Name = "Laser (Legacy Simulated)" },
                "IoOutput" => new SimulatedIoNode { Name = "IO Output (Legacy Simulated)" },
                "IoInput" => new SimulatedIoNode { Name = "IO Input (Legacy Simulated)" },
                "Delay" => new DelayNode(),
                _ => null
            };
        }
    }
}
