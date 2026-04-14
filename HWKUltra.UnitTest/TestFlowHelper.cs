using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Services;
using HWKUltra.Flow.Utils;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Test flow helper - creates valid test flows using existing node types
    /// </summary>
    public static class TestFlowHelper
    {
        /// <summary>
        /// Create a test AOI flow using valid node types only
        /// </summary>
        public static FlowDefinition CreateTestAoiFlow()
        {
            var flow = new FlowDefinition
            {
                Id = "test-aoi-flow",
                Name = "Test AOI Flow",
                Description = "Motion -> Camera -> Result"
            };

            // 1. Home axis
            var homeNode = new NodeDefinition
            {
                Id = "home",
                Type = "AxisHome",
                Name = "Home X",
                X = 100, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["AxisName"] = "X",
                    ["HomeMode"] = "Auto"
                }
            };
            flow.Nodes.Add(homeNode);

            // 2. Move to position
            var moveNode = new NodeDefinition
            {
                Id = "move_to_pos",
                Type = "AxisMoveAbs",
                Name = "Move to Position",
                X = 250, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["AxisName"] = "X",
                    ["Position"] = "100",
                    ["Velocity"] = "50000"
                }
            };
            flow.Nodes.Add(moveNode);

            // 3. Wait for stabilization
            var delayNode = new NodeDefinition
            {
                Id = "wait_stable",
                Type = "Delay",
                Name = "Wait Stabilization",
                X = 400, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["Duration"] = "100"
                }
            };
            flow.Nodes.Add(delayNode);

            // 4. Camera trigger
            var cameraNode = new NodeDefinition
            {
                Id = "capture",
                Type = "CameraTrigger",
                Name = "Trigger Camera",
                X = 550, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["CameraId"] = "Cam1",
                    ["TriggerMode"] = "Software"
                }
            };
            flow.Nodes.Add(cameraNode);

            // 5. OK handling - IO output
            var okNode = new NodeDefinition
            {
                Id = "ok_output",
                Type = "DigitalOutput",
                Name = "OK Signal Output",
                X = 700, Y = 50,
                Properties = new Dictionary<string, string>
                {
                    ["Port"] = "1",
                    ["Value"] = "true",
                    ["Duration"] = "100"
                }
            };
            flow.Nodes.Add(okNode);

            // 6. NG handling - IO output
            var ngNode = new NodeDefinition
            {
                Id = "ng_output",
                Type = "DigitalOutput",
                Name = "NG Signal Output",
                X = 700, Y = 150,
                Properties = new Dictionary<string, string>
                {
                    ["Port"] = "2",
                    ["Value"] = "true",
                    ["Duration"] = "100"
                }
            };
            flow.Nodes.Add(ngNode);

            // Set start node
            flow.StartNodeId = homeNode.Id;

            // Create connections
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = homeNode.Id,
                TargetNodeId = moveNode.Id
            });
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = moveNode.Id,
                TargetNodeId = delayNode.Id
            });
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = delayNode.Id,
                TargetNodeId = cameraNode.Id
            });
            // Branch based on capture result (simulated always OK for now)
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = cameraNode.Id,
                TargetNodeId = okNode.Id,
                Condition = "OK"
            });
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = cameraNode.Id,
                TargetNodeId = ngNode.Id,
                Condition = "NG"
            });

            return flow;
        }

        /// <summary>
        /// Create a flow testing logic nodes (Loop and Branch)
        /// </summary>
        public static FlowDefinition CreateLogicTestFlow()
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
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "branch", TargetNodeId = "loop", Condition = "True" });
            flow.Connections.Add(new ConnectionDefinition { SourceNodeId = "branch", TargetNodeId = "end", Condition = "False" });

            return flow;
        }

        /// <summary>
        /// Create a flow testing new node types (Motion, Camera, IO)
        /// </summary>
        public static FlowDefinition CreateNewNodesTestFlow()
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

        /// <summary>
        /// Demo node templates
        /// </summary>
        public static void DemoNodeTemplates()
        {
            Console.WriteLine("\n========== Node Template Demo ==========");

            var templates = NodeTemplateProvider.GetTemplates();

            foreach (var template in templates)
            {
                Console.WriteLine($"\n[{template.DisplayName}]");
                Console.WriteLine($"  Type: {template.Type}");
                Console.WriteLine($"  Category: {template.Category}");
                Console.WriteLine($"  Property count: {template.Properties.Count}");
            }

            Console.WriteLine($"\nTotal {templates.Count} node templates");
            Console.WriteLine("========== Demo End ==========");
        }

        /// <summary>
        /// Demo flow execution
        /// </summary>
        public static async Task DemoExecuteFlow()
        {
            Console.WriteLine("========== Flow Execution Demo ==========");

            // Create flow definition
            var flowDef = CreateTestAoiFlow();
            Console.WriteLine($"Flow created: {flowDef.Name}");
            Console.WriteLine($"Node count: {flowDef.Nodes.Count}");
            Console.WriteLine($"Connection count: {flowDef.Connections.Count}");

            // Save to JSON (visual editor can load)
            var json = FlowSerializer.Serialize(flowDef);
            Console.WriteLine("\nFlow definition JSON:");
            Console.WriteLine(json);

            // Create node factory (no actual controller, demo only)
            var nodeFactory = new DefaultNodeFactory(null);

            // Create flow manager
            var flowManager = new FlowManager(nodeFactory);
            flowManager.UpdateDefinition(flowDef);

            // Subscribe to events
            flowManager.NodeExecuting += (s, e) =>
                Console.WriteLine($"[Executing] {e.Node.Name}");
            flowManager.NodeExecuted += (s, e) =>
                Console.WriteLine($"[Completed] {e.Node.Name} - {(e.Result?.Success == true ? "Success" : "Failed")}");
            flowManager.FlowError += (s, e) =>
                Console.WriteLine($"[Error] {e.ErrorMessage}");
            flowManager.FlowCompleted += (s, e) =>
                Console.WriteLine($"[Flow Completed] Duration: {e.Instance.Duration?.TotalMilliseconds}ms");

            // Execute flow (without actual hardware)
            Console.WriteLine("\nStarting flow execution...");
            try
            {
                var result = await flowManager.ExecuteAsync(flowDef.Id);
                Console.WriteLine($"Execution result: {(result.Success ? "Success" : "Failed")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Execution exception: {ex.Message}");
            }

            Console.WriteLine("========== Demo End ==========");
        }

        /// <summary>
        /// Run multi-flow test
        /// </summary>
        public static async Task RunMultiFlowTest()
        {
            Console.WriteLine("\n========== Multi-Flow Execution Test ==========");

            // Create factory with simulation mode enabled
            var factory = new DefaultNodeFactory(null)
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
            var singleFlow = CreateTestAoiFlow();
            flowManager.UpdateDefinition(singleFlow);

            var result1 = await flowManager.ExecuteAsync(singleFlow.Id);
            Console.WriteLine($"✓ Single flow result: {(result1.Success ? "SUCCESS" : "FAILED")}");

            // Test 2: Execute multiple flows sequentially
            Console.WriteLine("\n----- Test 2: Sequential Multi-Flow Execution -----");

            // Create logic test flow
            var logicFlow = CreateLogicTestFlow();
            flowManager.UpdateDefinition(logicFlow);

            Console.WriteLine($"\nExecuting: {logicFlow.Name}");
            var result2 = await flowManager.ExecuteAsync(logicFlow.Id);
            Console.WriteLine($"  Result: {(result2.Success ? "SUCCESS" : "FAILED")}");

            // Test 3: Test concurrent flow execution
            Console.WriteLine("\n----- Test 3: Concurrent Flow Execution -----");

            var concurrentFlow = CreateTestAoiFlow();
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
    }
}
