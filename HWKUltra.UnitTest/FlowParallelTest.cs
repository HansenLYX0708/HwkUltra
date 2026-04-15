using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Nodes.Logic;
using HWKUltra.Flow.Services;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Tests for parallel execution, signals, locks, shared variables, sub-flow, and tray iterator.
    /// </summary>
    public class FlowParallelTest
    {
        public static async Task RunAllTests()
        {
            Console.WriteLine("\n========== Flow Parallel Extension Tests ==========");

            Test1_SharedFlowContext_Variables();
            Test2_SharedFlowContext_Locks();
            await Test3_SharedFlowContext_Signals();
            await Test4_SetGetSharedVariable_Nodes();
            await Test5_SignalNodes();
            await Test6_LockNodes();
            await Test7_SubFlowNode();
            await Test8_ParallelNode();
            await Test9_TrayIteratorNode_Simulation();
            Test10_AllNewNodesRegisteredInFactory();

            Console.WriteLine("========== Flow Parallel Extension Tests Complete ==========\n");
        }

        /// <summary>
        /// Test 1: SharedFlowContext - basic variable operations
        /// </summary>
        static void Test1_SharedFlowContext_Variables()
        {
            Console.WriteLine("\n----- Test 1: SharedFlowContext Variables -----");

            var shared = new SharedFlowContext();
            shared.SetVariable("key1", "hello");
            shared.SetVariable("key2", 42);
            shared.SetVariable("key3", true);

            var v1 = shared.GetVariable<string>("key1");
            var v2 = shared.GetVariable<int>("key2");
            var v3 = shared.GetVariable<bool>("key3");
            var v4 = shared.GetVariable<string>("nonexistent");

            Assert(v1 == "hello", $"key1: expected 'hello', got '{v1}'");
            Assert(v2 == 42, $"key2: expected 42, got {v2}");
            Assert(v3 == true, $"key3: expected true, got {v3}");
            Assert(v4 == null, $"nonexistent: expected null, got '{v4}'");

            shared.RemoveVariable("key1");
            Assert(shared.GetVariable<string>("key1") == null, "key1 should be null after removal");

            Console.WriteLine("✓ SharedFlowContext variables test passed");
        }

        /// <summary>
        /// Test 2: SharedFlowContext - named locks
        /// </summary>
        static void Test2_SharedFlowContext_Locks()
        {
            Console.WriteLine("\n----- Test 2: SharedFlowContext Locks -----");

            var shared = new SharedFlowContext();

            // Acquire lock
            var acquired = shared.AcquireLockAsync("TestLock", 1000).Result;
            Assert(acquired, "Should acquire lock");

            // Try to acquire same lock with short timeout (should fail)
            var acquired2 = shared.AcquireLockAsync("TestLock", 100).Result;
            Assert(!acquired2, "Should not acquire lock (already held)");

            // Release and re-acquire
            shared.ReleaseLock("TestLock");
            var acquired3 = shared.AcquireLockAsync("TestLock", 100).Result;
            Assert(acquired3, "Should acquire lock after release");
            shared.ReleaseLock("TestLock");

            Console.WriteLine("✓ SharedFlowContext locks test passed");
        }

        /// <summary>
        /// Test 3: SharedFlowContext - signals
        /// </summary>
        static async Task Test3_SharedFlowContext_Signals()
        {
            Console.WriteLine("\n----- Test 3: SharedFlowContext Signals -----");

            var shared = new SharedFlowContext();

            // Set signal before waiting (should return immediately)
            shared.SetSignal("ready", "go");
            var value = await shared.WaitForSignalAsync("ready", 1000);
            Assert(value?.ToString() == "go", $"Signal value: expected 'go', got '{value}'");
            Assert(shared.IsSignalSet("ready"), "Signal should still be set");

            // Reset and verify
            shared.ResetSignal("ready");
            Assert(!shared.IsSignalSet("ready"), "Signal should be reset");

            // Test async wait: set signal from another task
            var waitTask = Task.Run(async () =>
            {
                await Task.Delay(100);
                shared.SetSignal("async_signal", "hello_from_task");
            });

            var asyncValue = await shared.WaitForSignalAsync("async_signal", 5000);
            Assert(asyncValue?.ToString() == "hello_from_task", $"Async signal: expected 'hello_from_task', got '{asyncValue}'");
            await waitTask;

            // Test timeout (signal never set, should throw OperationCanceledException)
            bool timeoutOccurred = false;
            try
            {
                await shared.WaitForSignalAsync("timeout_test_never_set", 100);
            }
            catch (OperationCanceledException)
            {
                timeoutOccurred = true;
            }
            Assert(timeoutOccurred, "Timeout should throw OperationCanceledException");

            Console.WriteLine("✓ SharedFlowContext signals test passed");
        }

        /// <summary>
        /// Test 4: SetSharedVariable + GetSharedVariable nodes
        /// </summary>
        static async Task Test4_SetGetSharedVariable_Nodes()
        {
            Console.WriteLine("\n----- Test 4: Set/Get SharedVariable Nodes -----");

            var shared = new SharedFlowContext();
            var context = new FlowContext { SharedContext = shared };

            // Set shared variable
            var setNode = new SetSharedVariableNode { Id = "set1" };
            context.Variables["set1:Key"] = "stage_status";
            context.Variables["set1:Value"] = "active";
            var setResult = await setNode.ExecuteAsync(context);
            Assert(setResult.Success, $"SetSharedVariable should succeed: {setResult.ErrorMessage}");

            // Get shared variable
            var getNode = new GetSharedVariableNode { Id = "get1" };
            context.Variables["get1:Key"] = "stage_status";
            var getResult = await getNode.ExecuteAsync(context);
            Assert(getResult.Success, $"GetSharedVariable should succeed: {getResult.ErrorMessage}");

            var retrieved = context.GetNodeOutput<string>("get1", "Value");
            Assert(retrieved == "active", $"Expected 'active', got '{retrieved}'");

            Console.WriteLine("✓ Set/Get SharedVariable nodes test passed");
        }

        /// <summary>
        /// Test 5: WaitForSignal + SetSignal nodes
        /// </summary>
        static async Task Test5_SignalNodes()
        {
            Console.WriteLine("\n----- Test 5: Signal Nodes -----");

            var shared = new SharedFlowContext();

            // SetSignal
            var ctx1 = new FlowContext { SharedContext = shared };
            var setSignalNode = new SetSignalNode { Id = "ss1" };
            ctx1.Variables["ss1:SignalName"] = "stage_a_done";
            ctx1.Variables["ss1:Value"] = "completed";
            var r1 = await setSignalNode.ExecuteAsync(ctx1);
            Assert(r1.Success, "SetSignal should succeed");

            // WaitForSignal (signal already set, should return immediately)
            var ctx2 = new FlowContext { SharedContext = shared };
            var waitNode = new WaitForSignalNode { Id = "ws1" };
            ctx2.Variables["ws1:SignalName"] = "stage_a_done";
            ctx2.Variables["ws1:TimeoutMs"] = "5000";
            var r2 = await waitNode.ExecuteAsync(ctx2);
            Assert(r2.Success, "WaitForSignal should succeed");
            Assert(r2.BranchLabel == "Received", $"Expected branch 'Received', got '{r2.BranchLabel}'");

            var signalValue = ctx2.GetNodeOutput<string>("ws1", "Value");
            Assert(signalValue == "completed", $"Expected 'completed', got '{signalValue}'");

            // Timeout test (use a signal that was never set)
            var ctx3 = new FlowContext { SharedContext = shared };
            var waitNode2 = new WaitForSignalNode { Id = "ws2" };
            ctx3.Variables["ws2:SignalName"] = "signal_that_never_gets_set";
            ctx3.Variables["ws2:TimeoutMs"] = "100";
            var r3 = await waitNode2.ExecuteAsync(ctx3);
            Assert(r3.BranchLabel == "TimedOut", $"Expected branch 'TimedOut', got '{r3.BranchLabel}'");

            Console.WriteLine("✓ Signal nodes test passed");
        }

        /// <summary>
        /// Test 6: AcquireLock + ReleaseLock nodes
        /// </summary>
        static async Task Test6_LockNodes()
        {
            Console.WriteLine("\n----- Test 6: Lock Nodes -----");

            var shared = new SharedFlowContext();

            // Acquire lock
            var ctx1 = new FlowContext { SharedContext = shared };
            var acquireNode = new AcquireLockNode { Id = "al1" };
            ctx1.Variables["al1:LockName"] = "InspectionZone";
            var r1 = await acquireNode.ExecuteAsync(ctx1);
            Assert(r1.BranchLabel == "Acquired", $"Expected 'Acquired', got '{r1.BranchLabel}'");

            // Try acquire same lock (should timeout)
            var ctx2 = new FlowContext { SharedContext = shared };
            var acquireNode2 = new AcquireLockNode { Id = "al2" };
            ctx2.Variables["al2:LockName"] = "InspectionZone";
            ctx2.Variables["al2:TimeoutMs"] = "100";
            var r2 = await acquireNode2.ExecuteAsync(ctx2);
            Assert(r2.BranchLabel == "TimedOut", $"Expected 'TimedOut', got '{r2.BranchLabel}'");

            // Release lock
            var ctx3 = new FlowContext { SharedContext = shared };
            var releaseNode = new ReleaseLockNode { Id = "rl1" };
            ctx3.Variables["rl1:LockName"] = "InspectionZone";
            var r3 = await releaseNode.ExecuteAsync(ctx3);
            Assert(r3.Success, "ReleaseLock should succeed");

            // Now another can acquire
            var ctx4 = new FlowContext { SharedContext = shared };
            var acquireNode3 = new AcquireLockNode { Id = "al3" };
            ctx4.Variables["al3:LockName"] = "InspectionZone";
            ctx4.Variables["al3:TimeoutMs"] = "100";
            var r4 = await acquireNode3.ExecuteAsync(ctx4);
            Assert(r4.BranchLabel == "Acquired", $"Expected 'Acquired' after release, got '{r4.BranchLabel}'");
            shared.ReleaseLock("InspectionZone");

            Console.WriteLine("✓ Lock nodes test passed");
        }

        /// <summary>
        /// Test 7: SubFlowNode - execute a child flow definition
        /// </summary>
        static async Task Test7_SubFlowNode()
        {
            Console.WriteLine("\n----- Test 7: SubFlowNode -----");

            // Create a simple child flow JSON
            var childFlow = new FlowDefinition
            {
                Name = "ChildFlow",
                Description = "Simple delay flow for testing",
                StartNodeId = "child_delay"
            };
            childFlow.Nodes.Add(new NodeDefinition
            {
                Id = "child_delay",
                Type = "Delay",
                Name = "Child Delay",
                Properties = new Dictionary<string, string> { { "Duration", "10" } }
            });

            // Save to temp file
            var tempPath = Path.Combine(Path.GetTempPath(), "test_child_flow.json");
            HWKUltra.Flow.Utils.FlowSerializer.SaveToFile(childFlow, tempPath);

            try
            {
                var factory = new DefaultNodeFactory();
                var context = new FlowContext
                {
                    NodeFactory = factory,
                    SharedContext = new SharedFlowContext()
                };

                var subFlowNode = new SubFlowNode { Id = "sf1" };
                context.Variables["sf1:FlowPath"] = tempPath;

                var result = await subFlowNode.ExecuteAsync(context);
                Assert(result.Success, $"SubFlow should succeed: {result.ErrorMessage}");

                var success = context.GetNodeOutput<bool>("sf1", "Success");
                Assert(success, "SubFlow output Success should be true");

                var duration = context.GetNodeOutput<int>("sf1", "Duration");
                Assert(duration >= 0, $"Duration should be >= 0, got {duration}");

                Console.WriteLine($"  SubFlow executed in {duration}ms");
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }

            Console.WriteLine("✓ SubFlowNode test passed");
        }

        /// <summary>
        /// Test 8: ParallelNode - run multiple flows concurrently
        /// </summary>
        static async Task Test8_ParallelNode()
        {
            Console.WriteLine("\n----- Test 8: ParallelNode -----");

            // Create two child flows
            var flow1 = new FlowDefinition
            {
                Name = "ParallelChild1",
                StartNodeId = "d1"
            };
            flow1.Nodes.Add(new NodeDefinition
            {
                Id = "d1", Type = "Delay", Name = "Delay1",
                Properties = new Dictionary<string, string> { { "Duration", "50" } }
            });

            var flow2 = new FlowDefinition
            {
                Name = "ParallelChild2",
                StartNodeId = "d2"
            };
            flow2.Nodes.Add(new NodeDefinition
            {
                Id = "d2", Type = "Delay", Name = "Delay2",
                Properties = new Dictionary<string, string> { { "Duration", "50" } }
            });

            var flow3 = new FlowDefinition
            {
                Name = "ParallelChild3",
                StartNodeId = "d3"
            };
            flow3.Nodes.Add(new NodeDefinition
            {
                Id = "d3", Type = "Delay", Name = "Delay3",
                Properties = new Dictionary<string, string> { { "Duration", "50" } }
            });

            var path1 = Path.Combine(Path.GetTempPath(), "test_parallel1.json");
            var path2 = Path.Combine(Path.GetTempPath(), "test_parallel2.json");
            var path3 = Path.Combine(Path.GetTempPath(), "test_parallel3.json");
            HWKUltra.Flow.Utils.FlowSerializer.SaveToFile(flow1, path1);
            HWKUltra.Flow.Utils.FlowSerializer.SaveToFile(flow2, path2);
            HWKUltra.Flow.Utils.FlowSerializer.SaveToFile(flow3, path3);

            try
            {
                var factory = new DefaultNodeFactory();
                var context = new FlowContext
                {
                    NodeFactory = factory,
                    SharedContext = new SharedFlowContext()
                };

                var parallelNode = new ParallelNode { Id = "p1" };
                context.Variables["p1:FlowPaths"] = $"{path1},{path2},{path3}";
                context.Variables["p1:WaitMode"] = "All";

                var startTime = DateTime.UtcNow;
                var result = await parallelNode.ExecuteAsync(context);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                Assert(result.Success, $"Parallel should succeed: {result.ErrorMessage}");

                var completed = context.GetNodeOutput<int>("p1", "CompletedCount");
                var total = context.GetNodeOutput<int>("p1", "TotalCount");
                var allSuccess = context.GetNodeOutput<bool>("p1", "AllSuccess");

                Assert(completed == 3, $"CompletedCount: expected 3, got {completed}");
                Assert(total == 3, $"TotalCount: expected 3, got {total}");
                Assert(allSuccess, "AllSuccess should be true");

                // Parallel execution should be faster than sequential (3 * 50ms)
                Console.WriteLine($"  3 parallel flows completed in {elapsed:F0}ms (sequential would be ~150ms)");
                Assert(elapsed < 140, $"Parallel should be faster than sequential, took {elapsed:F0}ms");
            }
            finally
            {
                if (File.Exists(path1)) File.Delete(path1);
                if (File.Exists(path2)) File.Delete(path2);
                if (File.Exists(path3)) File.Delete(path3);
            }

            Console.WriteLine("✓ ParallelNode test passed");
        }

        /// <summary>
        /// Test 9: TrayIterator simulation
        /// </summary>
        static async Task Test9_TrayIteratorNode_Simulation()
        {
            Console.WriteLine("\n----- Test 9: TrayIterator Simulation -----");

            var node = new HWKUltra.Flow.Nodes.Tray.Simulation.SimTrayIteratorNode { Id = "ti1" };
            var context = new FlowContext();
            context.Variables["ti1:InstanceName"] = "TestTray";
            context.Variables["ti1:FilterState"] = "-1";
            context.Variables["ti1:Reset"] = "true";

            int count = 0;
            bool firstCall = true;
            while (true)
            {
                var result = await node.ExecuteAsync(context);
                Assert(result.Success, $"TrayIterator should succeed: {result.ErrorMessage}");

                // Clear Reset after first call so it doesn't reset every iteration
                if (firstCall) { context.Variables["ti1:Reset"] = "false"; firstCall = false; }

                if (result.BranchLabel == "Done")
                    break;

                Assert(result.BranchLabel == "Next", $"Expected 'Next', got '{result.BranchLabel}'");
                count++;

                if (count > 20) throw new Exception("TrayIterator infinite loop detected");
            }

            Assert(count == 16, $"Expected 16 slots (4x4), got {count}");
            Console.WriteLine($"  Iterated {count} slots");

            Console.WriteLine("✓ TrayIterator simulation test passed");
        }

        /// <summary>
        /// Test 10: All new node types registered in DefaultNodeFactory
        /// </summary>
        static void Test10_AllNewNodesRegisteredInFactory()
        {
            Console.WriteLine("\n----- Test 10: Factory Registration -----");

            var factory = new DefaultNodeFactory();
            var newTypes = new[]
            {
                "SubFlow", "Parallel", "SetSignal", "WaitForSignal",
                "AcquireLock", "ReleaseLock", "SetSharedVariable", "GetSharedVariable",
                "TrayIterator"
            };

            foreach (var type in newTypes)
            {
                try
                {
                    var node = factory.CreateNode(type, new Dictionary<string, string>());
                    Assert(node != null, $"Factory should create {type}");
                    Console.WriteLine($"  ✓ {type} → {node.GetType().Name}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Factory failed to create '{type}': {ex.Message}");
                }
            }

            // Also test simulated factory
            var simFactory = new DefaultNodeFactory();
            foreach (var type in newTypes)
            {
                var node = simFactory.CreateNode(type, new Dictionary<string, string>(), true);
                Assert(node != null, $"Simulated factory should create {type}");
            }

            Console.WriteLine("✓ Factory registration test passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"ASSERTION FAILED: {message}");
        }
    }
}
