using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Services;
using HWKUltra.Flow.Utils;

namespace HWKUltra.Flow.Examples
{
    /// <summary>
    /// Multi-flow execution test - validates concurrent flow execution with new node structure
    /// </summary>
    public class MultiFlowTest
    {
        /// <summary>
        /// Load test flows from JSON file
        /// </summary>
        public static List<FlowDefinition> LoadTestFlows(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Test flows file not found: {filePath}");
            }

            var json = File.ReadAllText(filePath);

            var flowCollection = System.Text.Json.JsonSerializer.Deserialize(json, FlowJsonContext.Default.FlowCollection);
            return flowCollection?.flows ?? new List<FlowDefinition>();
        }

        /// <summary>
        /// Run multi-flow test
        /// </summary>
        public static async Task RunMultiFlowTest()
        {
            Console.WriteLine("\n========== Multi-Flow Execution Test ==========");

            // Create factory with simulation mode enabled
            var factory = new DefaultNodeFactory(null)  // No real motion controller
            {
                UseSimulation = true
            };

            var flowManager = new FlowManager(factory);

            // Subscribe to events for monitoring
            flowManager.NodeExecuting += (s, e) =>
                Console.WriteLine($"  [Flow {e.Context.InstanceId}] Executing: {e.Node.Name}");

            flowManager.NodeExecuted += (s, e) =>
                Console.WriteLine($"  [Flow {e.Context.InstanceId}] Completed: {e.Node.Name} - {(e.Result?.Success == true ? "Success" : "Failed")}");

            flowManager.FlowCompleted += (s, e) =>
                Console.WriteLine($"  [Flow {e.Instance.Id}] Flow Completed! Duration: {e.Instance.Duration?.TotalMilliseconds:F1}ms");

            // Test 1: Execute single flow
            Console.WriteLine("\n----- Test 1: Single Flow Execution -----");
            var singleFlow = AoiFlowExample.CreateAoiInspectionFlow();
            flowManager.UpdateDefinition(singleFlow);

            var result1 = await flowManager.ExecuteAsync(singleFlow.Id);
            Console.WriteLine($"✓ Single flow result: {(result1.Success ? "SUCCESS" : "FAILED")}");

            // Test 2: Execute multiple flows sequentially
            Console.WriteLine("\n----- Test 2: Sequential Multi-Flow Execution -----");
            var testFilePath = Path.Combine(AppContext.BaseDirectory, "Examples", "TestFlows.json");
            
            // If file doesn't exist in bin, try source location
            if (!File.Exists(testFilePath))
            {
                testFilePath = @"g:\projects\AOIPlatform\HwkUltra_g\HWKUltra.Flow\Examples\TestFlows.json";
            }

            if (File.Exists(testFilePath))
            {
                var flows = LoadTestFlows(testFilePath);
                Console.WriteLine($"Loaded {flows.Count} flows from configuration");

                foreach (var flow in flows)
                {
                    flowManager.UpdateDefinition(flow);
                    Console.WriteLine($"\nExecuting: {flow.Name}");
                    
                    var result = await flowManager.ExecuteAsync(flow.Id);
                    Console.WriteLine($"  Result: {(result.Success ? "SUCCESS" : "FAILED")}");
                    
                    if (!result.Success && result.ErrorMessage != null)
                    {
                        Console.WriteLine($"  Error: {result.ErrorMessage}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Test flows file not found: {testFilePath}");
                Console.WriteLine("Using programmatic flows instead...");

                // Create additional test flows programmatically
                var logicFlow = CreateLogicTestFlow();
                flowManager.UpdateDefinition(logicFlow);

                Console.WriteLine($"\nExecuting: {logicFlow.Name}");
                var result = await flowManager.ExecuteAsync(logicFlow.Id);
                Console.WriteLine($"  Result: {(result.Success ? "SUCCESS" : "FAILED")}");
            }

            // Test 3: Test concurrent flow execution (demonstrates defensive copying)
            Console.WriteLine("\n----- Test 3: Concurrent Flow Execution (Protective Copy Test) -----");
            
            var concurrentFlow = AoiFlowExample.CreateAoiInspectionFlow();
            concurrentFlow.Id = "concurrent-test-flow";
            concurrentFlow.Name = "Concurrent Test Flow";
            flowManager.UpdateDefinition(concurrentFlow);

            // Execute same flow definition multiple times concurrently
            var tasks = new List<Task<FlowResult>>();
            for (int i = 0; i < 3; i++)
            {
                var context = new FlowContext();
                context.SetVariable("InstanceIndex", i);
                tasks.Add(flowManager.ExecuteAsync(concurrentFlow.Id, context));
            }

            var concurrentResults = await Task.WhenAll(tasks);
            
            Console.WriteLine($"Executed {concurrentResults.Length} concurrent instances");
            for (int i = 0; i < concurrentResults.Length; i++)
            {
                Console.WriteLine($"  Instance {i}: {(concurrentResults[i].Success ? "SUCCESS" : "FAILED")}");
            }

            // Test 4: Test new node types
            Console.WriteLine("\n----- Test 4: New Node Types Test -----");
            var newNodesFlow = CreateNewNodesTestFlow();
            flowManager.UpdateDefinition(newNodesFlow);

            Console.WriteLine($"Executing: {newNodesFlow.Name}");
            var newNodesResult = await flowManager.ExecuteAsync(newNodesFlow.Id);
            Console.WriteLine($"  Result: {(newNodesResult.Success ? "SUCCESS" : "FAILED")}");

            Console.WriteLine("\n========== Multi-Flow Test Complete ==========");
        }

        /// <summary>
        /// Create a flow for testing logic nodes
        /// </summary>
        private static FlowDefinition CreateLogicTestFlow()
        {
            var flow = new FlowDefinition
            {
                Id = "logic-test-flow",
                Name = "Logic Nodes Test",
                Description = "Tests Branch and Loop nodes"
            };

            // Initialize
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "init",
                Type = "Delay",
                Name = "Initialize",
                X = 100, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["Duration"] = "50",
                    ["CanCancel"] = "false"
                }
            });

            // Loop
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "loop",
                Type = "Loop",
                Name = "Test Loop",
                X = 300, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["Iterations"] = "2"
                }
            });

            // Branch condition
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "branch",
                Type = "Branch",
                Name = "Test Branch",
                X = 500, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["Condition"] = "CurrentIteration",
                    ["Operator"] = "GreaterThan",
                    ["CompareValue"] = "0"
                }
            });

            // End
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "end",
                Type = "Delay",
                Name = "End",
                X = 700, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["Duration"] = "50"
                }
            });

            // Connections
            flow.StartNodeId = "init";
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "init", TargetNodeId = "loop" });
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "loop", TargetNodeId = "branch", Condition = "Continue" });
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "loop", TargetNodeId = "end", Condition = "Exit" });
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "branch", TargetNodeId = "loop", Condition = "Continue" });

            return flow;
        }

        /// <summary>
        /// Create a flow testing new node types
        /// </summary>
        private static FlowDefinition CreateNewNodesTestFlow()
        {
            var flow = new FlowDefinition
            {
                Id = "new-nodes-test",
                Name = "New Node Types Test",
                Description = "Tests new categorized nodes"
            };

            // Axis home
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "home",
                Type = "AxisHome",
                Name = "Home Axis",
                X = 100, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["AxisName"] = "X",
                    ["HomeMode"] = "Auto"
                }
            });

            // Move absolute
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "move",
                Type = "AxisMoveAbs",
                Name = "Move to Position",
                X = 300, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["AxisName"] = "X",
                    ["Position"] = "100",
                    ["Velocity"] = "30000"
                }
            });

            // Camera trigger
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "trigger",
                Type = "CameraTrigger",
                Name = "Trigger Camera",
                X = 500, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["CameraId"] = "Cam1",
                    ["TriggerMode"] = "Software"
                }
            });

            // Digital output
            flow.Nodes.Add(new NodeDefinition
            {
                Id = "output",
                Type = "DigitalOutput",
                Name = "Set Output",
                X = 700, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["Port"] = "1",
                    ["Value"] = "true",
                    ["Duration"] = "100"
                }
            });

            flow.StartNodeId = "home";
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "home", TargetNodeId = "move" });
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "move", TargetNodeId = "trigger" });
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "trigger", TargetNodeId = "output" });

            return flow;
        }

    }
}
