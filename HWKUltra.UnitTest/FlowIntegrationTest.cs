using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Services;
using HWKUltra.Flow.Utils;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Integration tests for the complete AOI inspection flow.
    /// Generates JSON flow files and executes them using all new node types:
    /// SubFlow, Parallel, TrayIterator, SetSignal, WaitForSignal,
    /// AcquireLock, ReleaseLock, SetSharedVariable, GetSharedVariable
    /// </summary>
    public static class FlowIntegrationTest
    {
        private static string _flowDir = null!;

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"ASSERT FAILED: {message}");
            Console.WriteLine($"  ✓ {message}");
        }

        public static async Task RunAllTests()
        {
            Console.WriteLine("\n========== Flow Integration Tests ==========");

            // Setup: create flow directory and generate JSON files
            _flowDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFlows");
            if (Directory.Exists(_flowDir))
                Directory.Delete(_flowDir, true);
            Directory.CreateDirectory(_flowDir);

            GenerateAllFlowFiles();
            Console.WriteLine($"  Flow files generated in: {_flowDir}");

            Console.WriteLine("\n----- Test 1: Barcode Sub-Flow -----");
            await Test1_BarcodeScanSubFlow();

            Console.WriteLine("\n----- Test 2: StageA Solo (TrayIterator + Lock + Signal) -----");
            await Test2_StageA_Solo();

            Console.WriteLine("\n----- Test 3: Parallel Dual-Stage (StageA + StageB ping-pong) -----");
            await Test3_ParallelDualStage();

            Console.WriteLine("\n----- Test 4: Full Main Flow (Home + Light + SubFlow + Parallel) -----");
            await Test4_FullMainFlow();

            Console.WriteLine("\n========== Flow Integration Tests Complete ==========");
        }

        #region Test Methods

        /// <summary>
        /// Test 1: Execute barcode scan sub-flow standalone.
        /// Covers: BarcodeScannerTrigger, BarcodeScannerGetLast, SetSharedVariable
        /// </summary>
        private static async Task Test1_BarcodeScanSubFlow()
        {
            var factory = new DefaultNodeFactory();
            var shared = new SharedFlowContext();
            var path = Path.Combine(_flowDir, "BarcodeScan.json");

            var definition = FlowSerializer.LoadFromFile(path)!;
            var engine = new FlowEngine(definition);

            foreach (var n in definition.Nodes)
            {
                var node = factory.CreateNode(n.Type, n.Properties);
                node.Id = n.Id;
                node.Name = n.Name;
                engine.RegisterNode(node);
            }

            var context = new FlowContext { SharedContext = shared, NodeFactory = factory };
            foreach (var n in definition.Nodes)
                foreach (var p in n.Properties)
                    context.Variables[$"{n.Id}:{p.Key}"] = p.Value;

            var result = await engine.ExecuteAsync(context);
            Assert(result.Success, "BarcodeScan sub-flow completed");
            Assert(shared.TryGetVariable<string>("ProductBarcode", out var barcode) && barcode == "SN12345",
                $"ProductBarcode = {barcode}");
        }

        /// <summary>
        /// Test 2: Execute StageA with background auto-acknowledge (simulates StageB side).
        /// Covers: TrayIterator (loop), AcquireLock, ReleaseLock, SetSignal, WaitForSignal,
        ///         SetSharedVariable, AxisMoveAbs, CameraGrab, Delay
        /// </summary>
        private static async Task Test2_StageA_Solo()
        {
            var factory = new DefaultNodeFactory();
            var shared = new SharedFlowContext();
            var path = Path.Combine(_flowDir, "StageA_Inspection.json");

            var definition = FlowSerializer.LoadFromFile(path)!;
            using var cts = new CancellationTokenSource();

            // Background task: auto-acknowledge SlotReady with SlotProcessed (simulating StageB)
            var autoAckTask = Task.Run(async () =>
            {
                for (int i = 0; i < 16; i++) // 4x4 tray
                {
                    try
                    {
                        await shared.WaitForSignalAsync("SlotReady", -1, cts.Token);
                        shared.ResetSignal("SlotReady");
                        await Task.Delay(5, cts.Token);
                        shared.SetSignal("SlotProcessed", "auto-ack");
                    }
                    catch (OperationCanceledException) { break; }
                }
            });

            var result = await ExecuteFlow(definition, factory, shared);
            cts.Cancel();
            try { await autoAckTask; } catch (OperationCanceledException) { }

            Assert(result.Success, "StageA flow completed");
            Assert(shared.TryGetVariable<string>("StageA_Status", out var status) && status == "Complete",
                $"StageA_Status = {status}");
        }

        /// <summary>
        /// Test 3: Run StageA and StageB in parallel manually (Task.WhenAll).
        /// Tests the ping-pong signal synchronization between two concurrent flows.
        /// Covers: Parallel execution, WaitForSignal+AutoReset, SetSignal, AcquireLock, 
        ///         ReleaseLock, TrayIterator (loop), GetSharedVariable
        /// </summary>
        private static async Task Test3_ParallelDualStage()
        {
            var factory = new DefaultNodeFactory();
            var shared = new SharedFlowContext();

            var defA = FlowSerializer.LoadFromFile(Path.Combine(_flowDir, "StageA_Inspection.json"))!;
            var defB = FlowSerializer.LoadFromFile(Path.Combine(_flowDir, "StageB_Measurement.json"))!;

            var startTime = DateTime.UtcNow;

            // Run both stages concurrently
            var taskA = Task.Run(async () => await ExecuteFlow(defA, factory, shared));
            var taskB = Task.Run(async () => await ExecuteFlow(defB, factory, shared));
            var results = await Task.WhenAll(taskA, taskB);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Console.WriteLine($"  Parallel execution completed in {duration:F0}ms");

            Assert(results[0].Success, "StageA parallel completed");
            Assert(results[1].Success, "StageB parallel completed");

            shared.TryGetVariable<string>("StageA_Status", out var stA);
            shared.TryGetVariable<string>("StageB_Status", out var stB);
            shared.TryGetVariable<string>("InspectionResult", out var final);

            Assert(stA == "Complete", $"StageA_Status = {stA}");
            Assert(stB == "Complete", $"StageB_Status = {stB}");
            Assert(final == "PASS", $"InspectionResult = {final}");
        }

        /// <summary>
        /// Test 4: Execute the full main flow using ParallelNode and SubFlowNode.
        /// The main flow: Home → LightOn → SubFlow(BarcodeScan) → Parallel(StageA,StageB)
        ///   → GetSharedVar → LightOff → DigitalOutput
        /// Covers: ALL node types in one orchestrated flow.
        /// </summary>
        private static async Task Test4_FullMainFlow()
        {
            var factory = new DefaultNodeFactory();
            var shared = new SharedFlowContext();
            var path = Path.Combine(_flowDir, "MainAoiInspection.json");

            var definition = FlowSerializer.LoadFromFile(path)!;
            var startTime = DateTime.UtcNow;

            var result = await ExecuteFlow(definition, factory, shared);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Console.WriteLine($"  Full main flow completed in {duration:F0}ms");

            Assert(result.Success, "Main AOI flow completed");

            shared.TryGetVariable<string>("ProductBarcode", out var barcode);
            shared.TryGetVariable<string>("StageA_Status", out var stA);
            shared.TryGetVariable<string>("StageB_Status", out var stB);
            shared.TryGetVariable<string>("InspectionResult", out var final);
            shared.TryGetVariable<string>("SystemStatus", out var sys);

            Assert(barcode == "SN12345", $"ProductBarcode = {barcode}");
            Assert(stA == "Complete", $"StageA_Status = {stA}");
            Assert(stB == "Complete", $"StageB_Status = {stB}");
            Assert(final == "PASS", $"InspectionResult = {final}");
            Assert(sys == "Done", $"SystemStatus = {sys}");
        }

        #endregion

        #region Helper

        /// <summary>
        /// Execute a flow definition with the given factory and shared context
        /// </summary>
        private static async Task<FlowResult> ExecuteFlow(
            FlowDefinition definition, DefaultNodeFactory factory, SharedFlowContext shared)
        {
            var engine = new FlowEngine(definition);

            foreach (var n in definition.Nodes)
            {
                var node = factory.CreateNode(n.Type, n.Properties);
                node.Id = n.Id;
                node.Name = n.Name;
                node.Description = n.Description;
                engine.RegisterNode(node);
            }

            var context = new FlowContext
            {
                SharedContext = shared,
                NodeFactory = factory
            };

            foreach (var n in definition.Nodes)
                foreach (var p in n.Properties)
                    context.Variables[$"{n.Id}:{p.Key}"] = p.Value;

            return await engine.ExecuteAsync(context);
        }

        #endregion

        #region Flow File Generation

        private static void GenerateAllFlowFiles()
        {
            GenerateBarcodeScanFlow();
            GenerateStageA_InspectionFlow();
            GenerateStageB_MeasurementFlow();
            GenerateMainAoiInspectionFlow();

            // Also copy to ConfigJson/Flow for UI usage
            var configFlowDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigJson", "Flow");
            Directory.CreateDirectory(configFlowDir);
            foreach (var file in Directory.GetFiles(_flowDir, "*.json"))
            {
                File.Copy(file, Path.Combine(configFlowDir, Path.GetFileName(file)), true);
            }
        }

        /// <summary>
        /// Barcode scanning sub-flow:
        ///   BarcodeScannerTrigger → BarcodeScannerGetLast → SetSharedVariable(ProductBarcode)
        /// </summary>
        private static void GenerateBarcodeScanFlow()
        {
            var def = new FlowDefinition
            {
                Id = "barcode-scan-001",
                Name = "Barcode Scan",
                Description = "Sub-flow: scan product barcode and store in shared context",
                StartNodeId = "bc-trigger",
                Nodes = new List<NodeDefinition>
                {
                    Node("bc-trigger", "BarcodeScannerTrigger", "Trigger Scanner", 50, 100,
                        ("InstanceName", "LeftScanner")),
                    Node("bc-getlast", "BarcodeScannerGetLast", "Get Barcode", 280, 100,
                        ("InstanceName", "LeftScanner")),
                    Node("bc-setvar", "SetSharedVariable", "Store Barcode", 510, 100,
                        ("Key", "ProductBarcode"), ("Value", "SN12345"))
                },
                Connections = new List<ConnectionDefinition>
                {
                    Conn("bc-trigger", "bc-getlast"),
                    Conn("bc-getlast", "bc-setvar")
                }
            };
            SaveFlow(def, "BarcodeScan.json");
        }

        /// <summary>
        /// Stage A: Visual inspection with tray iteration (ping-pong with StageB via signals).
        ///
        /// SetSharedVariable(Running) → TrayIterator(Reset=true)
        ///   --[Next]--> AcquireLock → AxisMoveAbs → CameraGrab → Delay
        ///     → SetSharedVariable(SlotVisionResult=OK) → ReleaseLock
        ///     → SetSignal(SlotReady) → WaitForSignal(SlotProcessed, AutoReset)
        ///     → loop back to TrayIterator
        ///   --[Done]--> SetSharedVariable(Complete) → SetSignal(StageA_AllDone)
        /// </summary>
        private static void GenerateStageA_InspectionFlow()
        {
            var def = new FlowDefinition
            {
                Id = "stage-a-inspection-001",
                Name = "Stage A - Visual Inspection",
                Description = "Camera inspection for each tray slot with interlock and signal sync",
                StartNodeId = "a-status",
                Nodes = new List<NodeDefinition>
                {
                    Node("a-status", "SetSharedVariable", "Set StageA Running", 50, 200,
                        ("Key", "StageA_Status"), ("Value", "Running")),
                    Node("a-iter", "TrayIterator", "Iterate Tray Slots", 280, 200,
                        ("InstanceName", "MainTray"), ("Reset", "true"), ("FilterState", "-1")),
                    Node("a-lock", "AcquireLock", "Lock Inspection Zone", 510, 200,
                        ("LockName", "InspectionZone"), ("TimeoutMs", "10000")),
                    Node("a-move", "AxisMoveAbs", "Move X to Inspect", 740, 200,
                        ("AxisName", "X"), ("Position", "100.0"), ("Velocity", "50000"), ("WaitForComplete", "true")),
                    Node("a-grab", "CameraGrab", "Grab Image", 970, 200,
                        ("CameraName", "MainCamera"), ("TimeoutMs", "5000")),
                    Node("a-delay", "Delay", "Image Processing", 1200, 200,
                        ("Duration", "10"), ("CanCancel", "true")),
                    Node("a-set-result", "SetSharedVariable", "Set Vision Result", 1430, 200,
                        ("Key", "SlotVisionResult"), ("Value", "OK")),
                    Node("a-unlock", "ReleaseLock", "Release Inspection Zone", 1660, 200,
                        ("LockName", "InspectionZone")),
                    Node("a-signal-slot", "SetSignal", "Signal Slot Ready", 1890, 200,
                        ("SignalName", "SlotReady"), ("Value", "inspected")),
                    Node("a-wait-ack", "WaitForSignal", "Wait Slot Processed", 2120, 200,
                        ("SignalName", "SlotProcessed"), ("TimeoutMs", "10000"), ("AutoReset", "true")),
                    // Done path
                    Node("a-done-status", "SetSharedVariable", "Set StageA Complete", 510, 400,
                        ("Key", "StageA_Status"), ("Value", "Complete")),
                    Node("a-signal-done", "SetSignal", "Signal StageA Done", 740, 400,
                        ("SignalName", "StageA_AllDone"), ("Value", "complete"))
                },
                Connections = new List<ConnectionDefinition>
                {
                    Conn("a-status", "a-iter"),
                    Conn("a-iter", "a-lock", "Next"),
                    Conn("a-lock", "a-move", "Acquired"),
                    Conn("a-move", "a-grab"),
                    Conn("a-grab", "a-delay"),
                    Conn("a-delay", "a-set-result"),
                    Conn("a-set-result", "a-unlock"),
                    Conn("a-unlock", "a-signal-slot"),
                    Conn("a-signal-slot", "a-wait-ack"),
                    Conn("a-wait-ack", "a-iter", "Received"),    // loop back
                    // Done branch
                    Conn("a-iter", "a-done-status", "Done"),
                    Conn("a-done-status", "a-signal-done")
                }
            };
            SaveFlow(def, "StageA_Inspection.json");
        }

        /// <summary>
        /// Stage B: Measurement with signal sync from Stage A.
        ///
        /// SetSharedVariable(Running) → TrayIterator(Reset=true)
        ///   --[Next]--> WaitForSignal(SlotReady, AutoReset)
        ///     --[Received]--> GetSharedVariable(SlotVisionResult)
        ///       → AcquireLock(MeasurementZone) → AxisMoveAbs(Y) → Delay
        ///       → SetSharedVariable(SlotMeasureResult=OK) → ReleaseLock
        ///       → SetSignal(SlotProcessed) → loop back to TrayIterator
        ///   --[Done]--> SetSharedVariable(Complete) → SetSharedVariable(InspectionResult=PASS)
        /// </summary>
        private static void GenerateStageB_MeasurementFlow()
        {
            var def = new FlowDefinition
            {
                Id = "stage-b-measurement-001",
                Name = "Stage B - Measurement",
                Description = "Laser measurement for each tray slot, synchronized with Stage A via signals",
                StartNodeId = "b-status",
                Nodes = new List<NodeDefinition>
                {
                    Node("b-status", "SetSharedVariable", "Set StageB Running", 50, 200,
                        ("Key", "StageB_Status"), ("Value", "Running")),
                    Node("b-iter", "TrayIterator", "Iterate Tray Slots", 280, 200,
                        ("InstanceName", "MainTray"), ("Reset", "true"), ("FilterState", "-1")),
                    Node("b-wait-slot", "WaitForSignal", "Wait Slot Ready", 510, 200,
                        ("SignalName", "SlotReady"), ("TimeoutMs", "10000"), ("AutoReset", "true")),
                    Node("b-get-vision", "GetSharedVariable", "Get Vision Result", 740, 200,
                        ("Key", "SlotVisionResult"), ("DefaultValue", "UNKNOWN")),
                    Node("b-lock", "AcquireLock", "Lock Measurement Zone", 970, 200,
                        ("LockName", "MeasurementZone"), ("TimeoutMs", "10000")),
                    Node("b-move", "AxisMoveAbs", "Move Y to Measure", 1200, 200,
                        ("AxisName", "Y"), ("Position", "200.0"), ("Velocity", "50000"), ("WaitForComplete", "true")),
                    Node("b-delay", "Delay", "Measurement Processing", 1430, 200,
                        ("Duration", "10"), ("CanCancel", "true")),
                    Node("b-set-measure", "SetSharedVariable", "Set Measure Result", 1660, 200,
                        ("Key", "SlotMeasureResult"), ("Value", "OK")),
                    Node("b-unlock", "ReleaseLock", "Release Measurement Zone", 1890, 200,
                        ("LockName", "MeasurementZone")),
                    Node("b-signal-ack", "SetSignal", "Signal Slot Processed", 2120, 200,
                        ("SignalName", "SlotProcessed"), ("Value", "measured")),
                    // Done path
                    Node("b-done-status", "SetSharedVariable", "Set StageB Complete", 510, 400,
                        ("Key", "StageB_Status"), ("Value", "Complete")),
                    Node("b-set-final", "SetSharedVariable", "Set Final Result", 740, 400,
                        ("Key", "InspectionResult"), ("Value", "PASS"))
                },
                Connections = new List<ConnectionDefinition>
                {
                    Conn("b-status", "b-iter"),
                    Conn("b-iter", "b-wait-slot", "Next"),
                    Conn("b-wait-slot", "b-get-vision", "Received"),
                    Conn("b-get-vision", "b-lock"),
                    Conn("b-lock", "b-move", "Acquired"),
                    Conn("b-move", "b-delay"),
                    Conn("b-delay", "b-set-measure"),
                    Conn("b-set-measure", "b-unlock"),
                    Conn("b-unlock", "b-signal-ack"),
                    Conn("b-signal-ack", "b-iter"),    // loop back
                    // Done branch
                    Conn("b-iter", "b-done-status", "Done"),
                    Conn("b-done-status", "b-set-final")
                }
            };
            SaveFlow(def, "StageB_Measurement.json");
        }

        /// <summary>
        /// Main AOI Inspection orchestrator:
        ///   AxisHome(X) → AxisHome(Y) → LightOn → SetSharedVar(SystemStatus=Running)
        ///     → SubFlow(BarcodeScan.json)
        ///     → Parallel(StageA_Inspection.json, StageB_Measurement.json; WaitMode=All)
        ///     → GetSharedVariable(InspectionResult)
        ///     → LightOff → SetSharedVariable(SystemStatus=Done)
        ///     → DigitalOutput(InspectionDone=true)
        /// </summary>
        private static void GenerateMainAoiInspectionFlow()
        {
            var stageAPath = Path.Combine(_flowDir, "StageA_Inspection.json");
            var stageBPath = Path.Combine(_flowDir, "StageB_Measurement.json");
            var barcodePath = Path.Combine(_flowDir, "BarcodeScan.json");

            var def = new FlowDefinition
            {
                Id = "main-aoi-inspection-001",
                Name = "Main AOI Inspection",
                Description = "Full AOI inspection: home, barcode scan, parallel dual-stage inspection, result output",
                StartNodeId = "m-home-x",
                Nodes = new List<NodeDefinition>
                {
                    // Initialization
                    Node("m-home-x", "AxisHome", "Home X Axis", 50, 200,
                        ("AxisName", "X")),
                    Node("m-home-y", "AxisHome", "Home Y Axis", 280, 200,
                        ("AxisName", "Y")),
                    Node("m-light-on", "LightTurnOnOff", "Light On", 510, 200,
                        ("ChannelName", "MainLight"), ("TurnOn", "true")),
                    Node("m-set-running", "SetSharedVariable", "Set System Running", 740, 200,
                        ("Key", "SystemStatus"), ("Value", "Running")),

                    // Barcode scan sub-flow
                    Node("m-barcode", "SubFlow", "Scan Barcode", 970, 200,
                        ("FlowPath", barcodePath)),

                    // Parallel dual-stage inspection
                    Node("m-parallel", "Parallel", "Dual-Stage Inspection", 1200, 200,
                        ("FlowPaths", $"{stageAPath},{stageBPath}"),
                        ("WaitMode", "All"), ("TimeoutMs", "60000")),

                    // Result handling
                    Node("m-get-result", "GetSharedVariable", "Get Final Result", 1430, 200,
                        ("Key", "InspectionResult"), ("DefaultValue", "UNKNOWN")),
                    Node("m-light-off", "LightTurnOnOff", "Light Off", 1660, 200,
                        ("ChannelName", "MainLight"), ("TurnOn", "false")),
                    Node("m-set-done", "SetSharedVariable", "Set System Done", 1890, 200,
                        ("Key", "SystemStatus"), ("Value", "Done")),
                    Node("m-output", "DigitalOutput", "Output Done Signal", 2120, 200,
                        ("PointName", "InspectionDone"), ("Value", "true"))
                },
                Connections = new List<ConnectionDefinition>
                {
                    Conn("m-home-x", "m-home-y"),
                    Conn("m-home-y", "m-light-on"),
                    Conn("m-light-on", "m-set-running"),
                    Conn("m-set-running", "m-barcode"),
                    Conn("m-barcode", "m-parallel"),
                    Conn("m-parallel", "m-get-result"),
                    Conn("m-get-result", "m-light-off"),
                    Conn("m-light-off", "m-set-done"),
                    Conn("m-set-done", "m-output")
                }
            };
            SaveFlow(def, "MainAoiInspection.json");
        }

        #endregion

        #region Builder Helpers

        private static NodeDefinition Node(string id, string type, string name, double x, double y,
            params (string key, string value)[] props)
        {
            var node = new NodeDefinition
            {
                Id = id,
                Type = type,
                Name = name,
                X = x,
                Y = y,
                Properties = new Dictionary<string, string>()
            };
            foreach (var (key, value) in props)
                node.Properties[key] = value;
            return node;
        }

        private static ConnectionDefinition Conn(string from, string to, string? condition = null)
        {
            return new ConnectionDefinition
            {
                Id = $"c-{from}-{to}",
                SourceNodeId = from,
                TargetNodeId = to,
                Condition = condition
            };
        }

        private static void SaveFlow(FlowDefinition def, string filename)
        {
            var path = Path.Combine(_flowDir, filename);
            FlowSerializer.SaveToFile(def, path);
            Console.WriteLine($"    Generated: {filename} ({def.Nodes.Count} nodes, {def.Connections.Count} connections)");
        }

        #endregion
    }
}
