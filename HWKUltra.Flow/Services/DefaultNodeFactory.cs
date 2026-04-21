using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Motion.Real;
using HWKUltra.Flow.Nodes.Motion.Simulation;
using HWKUltra.Flow.Nodes.Camera.Real;
using HWKUltra.Flow.Nodes.Camera.Simulation;
using HWKUltra.Flow.Nodes.Measurement.Real;
using HWKUltra.Flow.Nodes.Measurement.Simulation;
using HWKUltra.Flow.Nodes.IO.Real;
using HWKUltra.Flow.Nodes.IO.Simulation;
using HWKUltra.Flow.Nodes.LightSource.Real;
using HWKUltra.Flow.Nodes.LightSource.Simulation;
using HWKUltra.Flow.Nodes.AutoFocus.Real;
using HWKUltra.Flow.Nodes.AutoFocus.Simulation;
using HWKUltra.Flow.Nodes.Tray.Real;
using HWKUltra.Flow.Nodes.Tray.Simulation;
using HWKUltra.Flow.Nodes.BarcodeScanner.Real;
using HWKUltra.Flow.Nodes.BarcodeScanner.Simulation;
using HWKUltra.Flow.Nodes.Communication.Real;
using HWKUltra.Flow.Nodes.Communication.Simulation;
using HWKUltra.Flow.Nodes.TeachData.Real;
using HWKUltra.Flow.Nodes.TeachData.Simulation;
using HWKUltra.Flow.Nodes.Logic;
using HWKUltra.Flow.Nodes.Advanced.Real;
using HWKUltra.Flow.Nodes.Vision;
using HWKUltra.Flow.Nodes.Session;
using HWKUltra.Core;
using HWKUltra.Vision.Abstractions;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.DeviceIO.Core;
using HWKUltra.LightSource.Core;
using HWKUltra.Camera.Core;
using HWKUltra.AutoFocus.Core;
using HWKUltra.Measurement.Core;
using HWKUltra.Tray.Core;
using HWKUltra.BarcodeScanner.Core;
using HWKUltra.Communication.Core;

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
        private readonly AutoFocusRouter? _autoFocusRouter;
        private readonly MeasurementRouter? _measurementRouter;
        private readonly TrayRouter? _trayRouter;
        private readonly BarcodeScannerRouter? _barcodeScannerRouter;
        private readonly CommunicationRouter? _communicationRouter;
        private readonly TeachDataService? _teachDataService;
        private readonly IInferenceEngine? _inferenceEngine;
        private readonly ITestRunStore? _testRunStore;

        public bool UseSimulation { get; set; } = false;

        public DefaultNodeFactory(
            MotionRouter? motionRouter = null,
            IORouter? ioRouter = null,
            LightSourceRouter? lightSourceRouter = null,
            CameraRouter? cameraRouter = null,
            AutoFocusRouter? autoFocusRouter = null,
            MeasurementRouter? measurementRouter = null,
            TrayRouter? trayRouter = null,
            BarcodeScannerRouter? barcodeScannerRouter = null,
            CommunicationRouter? communicationRouter = null,
            TeachDataService? teachDataService = null,
            IInferenceEngine? inferenceEngine = null,
            ITestRunStore? testRunStore = null)
        {
            _motionRouter = motionRouter;
            _ioRouter = ioRouter;
            _lightSourceRouter = lightSourceRouter;
            _cameraRouter = cameraRouter;
            _autoFocusRouter = autoFocusRouter;
            _measurementRouter = measurementRouter;
            _trayRouter = trayRouter;
            _barcodeScannerRouter = barcodeScannerRouter;
            _communicationRouter = communicationRouter;
            _teachDataService = teachDataService;
            _inferenceEngine = inferenceEngine;
            _testRunStore = testRunStore;
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

                // Measurement (null → auto simulation)
                "MeasurementOpen" => new MeasurementOpenNode(_measurementRouter),
                "MeasurementClose" => new MeasurementCloseNode(_measurementRouter),
                "MeasurementGetData" or "LaserTrigger" or "Laser" => new MeasurementGetDataNode(_measurementRouter),
                "MeasurementStartStorage" => new MeasurementStartStorageNode(_measurementRouter),
                "MeasurementStopStorage" => new MeasurementStopStorageNode(_measurementRouter),
                "MeasurementClearStorage" => new MeasurementClearStorageNode(_measurementRouter),
                "MeasurementGetTrendData" => new MeasurementGetTrendDataNode(_measurementRouter),
                "MeasurementSetSampling" => new MeasurementSetSamplingNode(_measurementRouter),
                "MeasurementControl" => new MeasurementControlNode(_measurementRouter),

                // IO (null → auto simulation)
                "DigitalOutput" or "IoOutput" => new DigitalOutputNode(_ioRouter),
                "DigitalInput" or "IoInput" => new DigitalInputNode(_ioRouter),

                // LightSource (null → auto simulation)
                "LightSetTriggerMode" or "LightTrigger" => new LightSetTriggerModeNode(_lightSourceRouter),
                "LightSetContinuousMode" or "LightContinuous" => new LightSetContinuousModeNode(_lightSourceRouter),
                "LightTurnOnOff" or "LightSwitch" => new LightTurnOnOffNode(_lightSourceRouter),

                // AutoFocus (null → auto simulation)
                "AutoFocusOpen" => new AutoFocusOpenNode(_autoFocusRouter),
                "AutoFocusClose" => new AutoFocusCloseNode(_autoFocusRouter),
                "AutoFocusEnable" => new AutoFocusEnableNode(_autoFocusRouter),
                "AutoFocusDisable" => new AutoFocusDisableNode(_autoFocusRouter),
                "AutoFocusLaserOn" => new AutoFocusLaserOnNode(_autoFocusRouter),
                "AutoFocusLaserOff" => new AutoFocusLaserOffNode(_autoFocusRouter),
                "AutoFocusGetStatus" => new AutoFocusGetStatusNode(_autoFocusRouter),
                "AutoFocusCommand" => new AutoFocusCommandNode(_autoFocusRouter),
                "AutoFocusReset" => new AutoFocusResetNode(_autoFocusRouter),

                // Tray (null → auto simulation)
                "TrayInit" => new TrayInitNode(_trayRouter),
                "TrayTeach" => new TrayTeachNode(_trayRouter),
                "TrayGetPosition" => new TrayGetPositionNode(_trayRouter),
                "TraySetSlotState" => new TraySetSlotStateNode(_trayRouter),
                "TrayGetSlotState" => new TrayGetSlotStateNode(_trayRouter),
                "TrayReset" => new TrayResetNode(_trayRouter),
                "TrayGetInfo" => new TrayGetInfoNode(_trayRouter),

                // BarcodeScanner (null → auto simulation)
                "BarcodeScannerOpen" => new BarcodeScannerOpenNode(_barcodeScannerRouter),
                "BarcodeScannerClose" => new BarcodeScannerCloseNode(_barcodeScannerRouter),
                "BarcodeScannerTrigger" => new BarcodeScannerTriggerNode(_barcodeScannerRouter),
                "BarcodeScannerGetLast" => new BarcodeScannerGetLastNode(_barcodeScannerRouter),

                // Communication (null → auto simulation)
                "CommunicationOpen" => new CommunicationOpenNode(_communicationRouter),
                "CommunicationClose" => new CommunicationCloseNode(_communicationRouter),
                "CommunicationStartScan" => new CommunicationStartScanNode(_communicationRouter),
                "CommunicationLoad" => new CommunicationLoadNode(_communicationRouter),
                "CommunicationUnload" => new CommunicationUnloadNode(_communicationRouter),
                "CommunicationComplete" => new CommunicationCompleteNode(_communicationRouter),
                "CommunicationAbort" => new CommunicationAbortNode(_communicationRouter),
                "CommunicationLogin" => new CommunicationLoginNode(_communicationRouter),

                // Tray Iterator
                "TrayIterator" => new TrayIteratorNode(_trayRouter),

                // TeachData (uses MotionRouter + TeachDataService)
                "MoveToTeachPosition" => new MoveToTeachPositionNode(_motionRouter, _teachDataService),
                "GetTeachPosition" => new GetTeachPositionNode(_teachDataService),
                "SetTeachPosition" => new SetTeachPositionNode(_teachDataService),

                // Logic Control - no hardware dependency
                "Delay" => new DelayNode(),
                "Branch" => new BranchNode(),
                "Loop" => new LoopNode(),
                "SubFlow" => new SubFlowNode(),
                "Parallel" => new ParallelNode(),
                "SetSignal" => new SetSignalNode(),
                "WaitForSignal" => new WaitForSignalNode(),
                "AcquireLock" => new AcquireLockNode(),
                "ReleaseLock" => new ReleaseLockNode(),
                "SetSharedVariable" => new SetSharedVariableNode(),
                "GetSharedVariable" => new GetSharedVariableNode(),
                "IncrementSharedVariable" => new IncrementSharedVariableNode(),

                // Vision (no hardware router; LogicNodeBase nodes are pure functions)
                "LoadImage" => new LoadImageNode(),
                "LoadImageFolder" => new LoadImageFolderNode(),
                "GetSharpnessLaplacian" => new GetSharpnessLaplacianNode(),
                "GetSharpnessVar" => new GetSharpnessVarNode(),
                "GetTenengrad" => new GetTenengradNode(),
                "CalibrateCameraMpp" => new CalibrateCameraMppNode(),
                "FindDatum" => new FindDatumNode(),
                "FindLaserDatum" => new FindLaserDatumNode(),
                "JudgeRowBarEmpty" => new JudgeRowBarEmptyNode(),
                "DLInference" => new InferenceNode(_inferenceEngine),

                // Session (test-run lifecycle, depends on ITestRunStore)
                "StartTrayRun" => new StartTrayRunNode(_testRunStore),
                "PopulateTrayReport" => new PopulateTrayReportNode(_testRunStore),
                "FinalizeTrayRun" => new FinalizeTrayRunNode(_testRunStore),

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

                // Measurement Simulation
                "MeasurementOpen" => new SimMeasurementOpenNode(),
                "MeasurementClose" => new SimMeasurementCloseNode(),
                "MeasurementGetData" or "LaserTrigger" or "Laser" => new SimMeasurementGetDataNode(),
                "MeasurementStartStorage" => new SimMeasurementStartStorageNode(),
                "MeasurementStopStorage" => new SimMeasurementStopStorageNode(),
                "MeasurementClearStorage" => new SimMeasurementClearStorageNode(),
                "MeasurementGetTrendData" => new SimMeasurementGetTrendDataNode(),
                "MeasurementSetSampling" => new SimMeasurementSetSamplingNode(),
                "MeasurementControl" => new SimMeasurementControlNode(),

                // IO Simulation
                "DigitalOutput" or "IoOutput" => new SimDigitalOutputNode(),
                "DigitalInput" or "IoInput" => new SimDigitalInputNode(),

                // LightSource Simulation
                "LightSetTriggerMode" or "LightTrigger" => new SimLightSetTriggerModeNode(),
                "LightSetContinuousMode" or "LightContinuous" => new SimLightSetContinuousModeNode(),
                "LightTurnOnOff" or "LightSwitch" => new SimLightTurnOnOffNode(),

                // AutoFocus Simulation
                "AutoFocusOpen" => new SimAutoFocusOpenNode(),
                "AutoFocusClose" => new SimAutoFocusCloseNode(),
                "AutoFocusEnable" => new SimAutoFocusEnableNode(),
                "AutoFocusDisable" => new SimAutoFocusDisableNode(),
                "AutoFocusLaserOn" => new SimAutoFocusLaserOnNode(),
                "AutoFocusLaserOff" => new SimAutoFocusLaserOffNode(),
                "AutoFocusGetStatus" => new SimAutoFocusGetStatusNode(),
                "AutoFocusCommand" => new SimAutoFocusCommandNode(),
                "AutoFocusReset" => new SimAutoFocusResetNode(),

                // Tray Simulation
                "TrayInit" => new SimTrayInitNode(),
                "TrayTeach" => new SimTrayTeachNode(),
                "TrayGetPosition" => new SimTrayGetPositionNode(),
                "TraySetSlotState" => new SimTraySetSlotStateNode(),
                "TrayGetSlotState" => new SimTrayGetSlotStateNode(),
                "TrayReset" => new SimTrayResetNode(),
                "TrayGetInfo" => new SimTrayGetInfoNode(),

                // BarcodeScanner Simulation
                "BarcodeScannerOpen" => new SimBarcodeScannerOpenNode(),
                "BarcodeScannerClose" => new SimBarcodeScannerCloseNode(),
                "BarcodeScannerTrigger" => new SimBarcodeScannerTriggerNode(),
                "BarcodeScannerGetLast" => new SimBarcodeScannerGetLastNode(),

                // Communication Simulation
                "CommunicationOpen" => new SimCommunicationOpenNode(),
                "CommunicationClose" => new SimCommunicationCloseNode(),
                "CommunicationStartScan" => new SimCommunicationStartScanNode(),
                "CommunicationLoad" => new SimCommunicationLoadNode(),
                "CommunicationUnload" => new SimCommunicationUnloadNode(),
                "CommunicationComplete" => new SimCommunicationCompleteNode(),
                "CommunicationAbort" => new SimCommunicationAbortNode(),
                "CommunicationLogin" => new SimCommunicationLoginNode(),

                // Tray Iterator Simulation
                "TrayIterator" => new SimTrayIteratorNode(),

                // TeachData Simulation
                "MoveToTeachPosition" => new SimMoveToTeachPositionNode(),
                "GetTeachPosition" => new SimGetTeachPositionNode(),
                "SetTeachPosition" => new SimSetTeachPositionNode(),

                // Logic Control - no hardware dependency
                "Delay" => new DelayNode(),
                "Branch" => new BranchNode(),
                "Loop" => new LoopNode(),
                "SubFlow" => new SubFlowNode(),
                "Parallel" => new ParallelNode(),
                "SetSignal" => new SetSignalNode(),
                "WaitForSignal" => new WaitForSignalNode(),
                "AcquireLock" => new AcquireLockNode(),
                "ReleaseLock" => new ReleaseLockNode(),
                "SetSharedVariable" => new SetSharedVariableNode(),
                "GetSharedVariable" => new GetSharedVariableNode(),
                "IncrementSharedVariable" => new IncrementSharedVariableNode(),

                // Vision Simulation (LogicNodeBase — pure image processing, no hardware to simulate;
                // raw algorithms still execute. InferenceNode falls back to simulation via null engine.)
                "LoadImage" => new LoadImageNode(),
                "LoadImageFolder" => new LoadImageFolderNode(),
                "GetSharpnessLaplacian" => new GetSharpnessLaplacianNode(),
                "GetSharpnessVar" => new GetSharpnessVarNode(),
                "GetTenengrad" => new GetTenengradNode(),
                "CalibrateCameraMpp" => new CalibrateCameraMppNode(),
                "FindDatum" => new FindDatumNode(),
                "FindLaserDatum" => new FindLaserDatumNode(),
                "JudgeRowBarEmpty" => new JudgeRowBarEmptyNode(),
                "DLInference" => new InferenceNode(null),

                // Session Simulation (null store → DeviceNodeBase runs ExecuteSimulatedAsync)
                "StartTrayRun" => new StartTrayRunNode(null),
                "PopulateTrayReport" => new PopulateTrayReportNode(null),
                "FinalizeTrayRun" => new FinalizeTrayRunNode(null),

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
