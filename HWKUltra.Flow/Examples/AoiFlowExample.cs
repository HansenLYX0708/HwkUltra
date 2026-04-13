using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Engine;
using HWKUltra.Flow.Models;
using HWKUltra.Flow.Nodes;
using HWKUltra.Flow.Services;
using HWKUltra.Flow.Utils;

namespace HWKUltra.Flow.Examples
{
    /// <summary>
    /// AOI inspection flow example
    /// </summary>
    public class AoiFlowExample
    {
        /// <summary>
        /// Create a simple AOI inspection flow
        /// </summary>
        public static FlowDefinition CreateAoiInspectionFlow()
        {
            var flow = new FlowDefinition
            {
                Name = "AOI Inspection Flow",
                Description = "Move -> Capture -> Inspect -> Result"
            };

            // 1. Move to inspection position
            var moveNode = new NodeDefinition
            {
                Id = "move_to_pos",
                Type = "Motion",
                Name = "Move to Position",
                X = 100, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["AxisName"] = "XY",
                    ["Position"] = "100",
                    ["Velocity"] = "50000"
                }
            };
            flow.Nodes.Add(moveNode);

            // 2. Wait for stabilization
            var delayNode = new NodeDefinition
            {
                Id = "wait_stable",
                Type = "Delay",
                Name = "Wait Stabilization",
                X = 300, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["Duration"] = "500"
                }
            };
            flow.Nodes.Add(delayNode);

            // 3. Camera capture
            var cameraNode = new NodeDefinition
            {
                Id = "capture",
                Type = "Camera",
                Name = "Camera Capture",
                X = 500, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["CameraId"] = "Cam1",
                    ["ExposureTime"] = "10000"
                }
            };
            flow.Nodes.Add(cameraNode);

            // 4. AOI inspection
            var inspectNode = new NodeDefinition
            {
                Id = "inspect",
                Type = "Inspection",
                Name = "AOI Inspection",
                X = 700, Y = 100,
                Properties = new Dictionary<string, string>
                {
                    ["RecipeName"] = "PCB_Inspect"
                }
            };
            flow.Nodes.Add(inspectNode);

            // 5. OK handling - IO output
            var okNode = new NodeDefinition
            {
                Id = "ok_output",
                Type = "IoOutput",
                Name = "OK Signal Output",
                X = 900, Y = 50,
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
                Type = "IoOutput",
                Name = "NG Signal Output",
                X = 900, Y = 150,
                Properties = new Dictionary<string, string>
                {
                    ["Port"] = "2",
                    ["Value"] = "true",
                    ["Duration"] = "100"
                }
            };
            flow.Nodes.Add(ngNode);

            // Set start node
            flow.StartNodeId = moveNode.Id;

            // Create connections
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
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = cameraNode.Id,
                TargetNodeId = inspectNode.Id
            });
            // Inspection result branches
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = inspectNode.Id,
                TargetNodeId = okNode.Id,
                Condition = "OK"
            });
            flow.Connections.Add(new ConnectionDefinition
            {
                SourceNodeId = inspectNode.Id,
                TargetNodeId = ngNode.Id,
                Condition = "NG"
            });

            return flow;
        }

        /// <summary>
        /// Demo how to use FlowManager to execute flow
        /// </summary>
        public static async Task DemoExecuteFlow()
        {
            Console.WriteLine("========== AOI Flow Execution Demo ==========");

            // Create flow definition
            var flowDef = CreateAoiInspectionFlow();
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
        /// Demo how to create and use node templates
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
    }
}
