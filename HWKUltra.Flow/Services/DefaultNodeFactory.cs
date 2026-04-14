using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Motion.Real;
using HWKUltra.Flow.Nodes.Motion.Simulation;
using HWKUltra.Flow.Nodes.Camera.Real;
using HWKUltra.Flow.Nodes.Camera.Simulation;
using HWKUltra.Flow.Nodes.Laser.Real;
using HWKUltra.Flow.Nodes.IO.Real;
using HWKUltra.Flow.Nodes.IO.Simulation;
using HWKUltra.Flow.Nodes.LightSource.Real;
using HWKUltra.Flow.Nodes.LightSource.Simulation;
using HWKUltra.Flow.Nodes.Logic;
using HWKUltra.Flow.Nodes.Advanced.Real;
using HWKUltra.Motion.Core;
using HWKUltra.DeviceIO.Core;
using HWKUltra.LightSource.Core;
using HWKUltra.Camera.Core;

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
        private readonly LightSourceRouter? _lightSourceRouter;
        private readonly CameraRouter? _cameraRouter;
        private readonly object? _laserService;   // TODO: Replace with ILaserService

        public bool UseSimulation { get; set; } = false;

        public DefaultNodeFactory(
            MotionRouter? motionRouter = null,
            IORouter? ioRouter = null,
            LightSourceRouter? lightSourceRouter = null,
            CameraRouter? cameraRouter = null,
            object? laserService = null)
        {
            _motionRouter = motionRouter;
            _ioRouter = ioRouter;
            _lightSourceRouter = lightSourceRouter;
            _cameraRouter = cameraRouter;
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
                "CameraTrigger" or "Camera" => new CameraTriggerNode(_cameraRouter),
                "CameraOpen" => new CameraOpenNode(_cameraRouter),
                "CameraClose" => new CameraCloseNode(_cameraRouter),
                "CameraGrab" => new CameraGrabNode(_cameraRouter),
                "CameraSetExposure" => new CameraSetExposureNode(_cameraRouter),
                "CameraSetGain" => new CameraSetGainNode(_cameraRouter),
                "CameraSetTriggerMode" => new CameraSetTriggerModeNode(_cameraRouter),

                // Laser (null → auto simulation)
                "LaserTrigger" or "Laser" => new LaserTriggerNode(_laserService),

                // IO (null → auto simulation)
                "DigitalOutput" or "IoOutput" => new DigitalOutputNode(_ioRouter),
                "DigitalInput" or "IoInput" => new DigitalInputNode(_ioRouter),

                // LightSource (null → auto simulation)
                "LightSetTriggerMode" or "LightTrigger" => new LightSetTriggerModeNode(_lightSourceRouter),
                "LightSetContinuousMode" or "LightContinuous" => new LightSetContinuousModeNode(_lightSourceRouter),
                "LightTurnOnOff" or "LightSwitch" => new LightTurnOnOffNode(_lightSourceRouter),

                // Logic Control - no hardware dependency
                "Delay" => new DelayNode(),
                "Branch" => new BranchNode(),
                "Loop" => new LoopNode(),

                // Advanced Features
                "OnTheFlyCapture" => new OnTheFlyCaptureNode(_motionRouter, _cameraRouter),

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
                // Motion Simulation
                "AxisHome" => new SimAxisHomeNode(),
                "AxisMoveAbs" => new SimAxisMoveAbsNode(),
                "AxisMoveRel" => new SimAxisMoveRelNode(),
                "AxisMoveVelocity" => new SimAxisMoveVelocityNode(),
                "AxisWaitInPos" => new SimAxisWaitInPosNode(),
                "GroupInterpolation" => new SimGroupInterpolationNode(),

                // Camera Simulation
                "CameraTrigger" or "Camera" => new SimCameraTriggerNode(),
                "CameraOpen" => new SimCameraOpenNode(),
                "CameraClose" => new SimCameraCloseNode(),
                "CameraGrab" => new SimCameraGrabNode(),
                "CameraSetExposure" => new SimCameraSetExposureNode(),
                "CameraSetGain" => new SimCameraSetGainNode(),
                "CameraSetTriggerMode" => new SimCameraSetTriggerModeNode(),

                // Laser Simulation (TODO: dedicated sim node)
                "LaserTrigger" or "Laser" => new LaserTriggerNode(null),

                // IO Simulation
                "DigitalOutput" or "IoOutput" => new SimDigitalOutputNode(),
                "DigitalInput" or "IoInput" => new SimDigitalInputNode(),

                // LightSource Simulation
                "LightSetTriggerMode" or "LightTrigger" => new SimLightSetTriggerModeNode(),
                "LightSetContinuousMode" or "LightContinuous" => new SimLightSetContinuousModeNode(),
                "LightTurnOnOff" or "LightSwitch" => new SimLightTurnOnOffNode(),

                // Logic Control - no hardware dependency
                "Delay" => new DelayNode(),
                "Branch" => new BranchNode(),
                "Loop" => new LoopNode(),

                // Advanced Simulation
                "OnTheFlyCapture" => new OnTheFlyCaptureNode(null, null, true),

                // Legacy compatibility
                "Motion" => new SimAxisMoveAbsNode() { Name = "Motion (Legacy Sim)" },
                "MotionGroup" => new SimGroupInterpolationNode() { Name = "MotionGroup (Legacy Sim)" },
                "WaitForAxis" => new SimAxisWaitInPosNode() { Name = "WaitForAxis (Legacy Sim)" },

                _ => throw new ArgumentException($"Unknown node type: {type}")
            };
        }
    }
}
