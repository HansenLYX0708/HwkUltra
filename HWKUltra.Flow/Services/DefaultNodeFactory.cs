using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes;
using HWKUltra.Motion.Abstractions;

namespace HWKUltra.Flow.Services
{
    /// <summary>
    /// Default node factory implementation
    /// </summary>
    public class DefaultNodeFactory : IFlowNodeFactory
    {
        private readonly IMotionController? _motionController;
        // TODO: Inject other device services
        // private readonly ICameraService _cameraService;
        // private readonly ILaserService _laserService;
        // private readonly IIoService _ioService;

        public DefaultNodeFactory(IMotionController? motionController = null)
        {
            _motionController = motionController;
        }

        public IFlowNode CreateNode(string type, Dictionary<string, string> properties)
        {
            IFlowNode node = type switch
            {
                "Motion" => _motionController != null
                    ? new MotionNode(_motionController)
                    : throw new InvalidOperationException("MotionController not available"),
                "MotionGroup" => _motionController != null
                    ? new MotionGroupNode(_motionController)
                    : throw new InvalidOperationException("MotionController not available"),
                "WaitForAxis" => new WaitForAxisNode(),
                "Camera" => new CameraNode(),
                "Inspection" => new InspectionNode(),
                "Laser" => new LaserMeasureNode(),
                "IoOutput" => new IoOutputNode(),
                "IoInput" => new IoInputNode(),
                "Delay" => new DelayNode(),
                _ => throw new ArgumentException($"Unknown node type: {type}")
            };

            // Restore node general configuration from properties
            if (properties.TryGetValue("Name", out var name) && !string.IsNullOrEmpty(name))
                node.Name = name;
            if (properties.TryGetValue("Description", out var description))
                node.Description = description;

            return node;
        }
    }
}
