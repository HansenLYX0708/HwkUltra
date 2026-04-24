using HWKUltra.Flow.Models;
using System.Text.Json;

namespace HWKUltra.Flow.Utils
{
    /// <summary>
    /// Flow serialization utility - uses source generator for AOT support
    /// </summary>
    public static class FlowSerializer
    {
        /// <summary>
        /// Serialize flow definition to JSON
        /// </summary>
        public static string Serialize(FlowDefinition definition)
        {
            return JsonSerializer.Serialize(definition, FlowJsonContext.Default.FlowDefinition);
        }

        /// <summary>
        /// Deserialize flow definition from JSON
        /// </summary>
        public static FlowDefinition? Deserialize(string json)
        {
            return JsonSerializer.Deserialize(json, FlowJsonContext.Default.FlowDefinition);
        }

        /// <summary>
        /// Load flow definition from file
        /// </summary>
        public static FlowDefinition? LoadFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var def = Deserialize(json);
            if (def != null)
                def.SourceFilePath = Path.GetFullPath(filePath);
            return def;
        }

        /// <summary>
        /// Save flow definition to file
        /// </summary>
        public static void SaveToFile(FlowDefinition definition, string filePath)
        {
            var json = Serialize(definition);
            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Node template - for visual editor node templates
    /// </summary>
    public class NodeTemplate
    {
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Motion, Camera, IO, Logic, etc.
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public string Color { get; set; } = "#2196F3"; // Default blue
        public List<PortDefinition> Inputs { get; set; } = new();
        public List<PortDefinition> Outputs { get; set; } = new();
        public List<PropertyDefinition> Properties { get; set; } = new();
    }

    public class PortDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = "flow"; // flow, data, trigger
        public bool Required { get; set; } = false;
    }

    public class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = "string"; // string, int, double, bool, enum, position
        public bool Required { get; set; } = false;
        public object? DefaultValue { get; set; }
        public string? Description { get; set; }
        public List<string>? Options { get; set; } // For enum type
    }

    /// <summary>
    /// Node template provider
    /// </summary>
    public class NodeTemplateProvider
    {
        public static List<NodeTemplate> GetTemplates()
        {
            return new List<NodeTemplate>
            {
                // Motion control
                new NodeTemplate
                {
                    Type = "Motion",
                    DisplayName = "Axis Motion",
                    Category = "Motion",
                    Color = "#FF5722",
                    Inputs = new List<PortDefinition> { new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" } },
                    Outputs = new List<PortDefinition> { new PortDefinition { Name = "Out", DisplayName = "Out", Type = "flow" } },
                    Properties = new List<PropertyDefinition>
                    {
                        new PropertyDefinition { Name = "AxisName", DisplayName = "Axis Name", Type = "string", Required = true, DefaultValue = "X" },
                        new PropertyDefinition { Name = "Position", DisplayName = "Position", Type = "double", Required = true },
                        new PropertyDefinition { Name = "Velocity", DisplayName = "Velocity", Type = "double", DefaultValue = 50000.0 }
                    }
                },
                new NodeTemplate
                {
                    Type = "MotionGroup",
                    DisplayName = "Multi-axis Interpolation",
                    Category = "Motion",
                    Color = "#E64A19",
                    Inputs = new List<PortDefinition> { new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" } },
                    Outputs = new List<PortDefinition> { new PortDefinition { Name = "Out", DisplayName = "Out", Type = "flow" } },
                    Properties = new List<PropertyDefinition>
                    {
                        new PropertyDefinition { Name = "GroupName", DisplayName = "Group Name", Type = "string", Required = true, DefaultValue = "XY" },
                        new PropertyDefinition { Name = "X", DisplayName = "X", Type = "double" },
                        new PropertyDefinition { Name = "Y", DisplayName = "Y", Type = "double" }
                    }
                },
                new NodeTemplate
                {
                    Type = "WaitForAxis",
                    DisplayName = "Wait For Axis",
                    Category = "Motion",
                    Color = "#FF9800",
                    Inputs = new List<PortDefinition> { new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" } },
                    Outputs = new List<PortDefinition> { new PortDefinition { Name = "Out", DisplayName = "Out", Type = "flow" } }
                },

                // Camera
                new NodeTemplate
                {
                    Type = "Camera",
                    DisplayName = "Camera Capture",
                    Category = "Camera",
                    Color = "#4CAF50",
                    Inputs = new List<PortDefinition> { new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" } },
                    Outputs = new List<PortDefinition>
                    {
                        new PortDefinition { Name = "Out", DisplayName = "Out", Type = "flow" },
                        new PortDefinition { Name = "Image", DisplayName = "Image", Type = "data" }
                    },
                    Properties = new List<PropertyDefinition>
                    {
                        new PropertyDefinition { Name = "CameraId", DisplayName = "Camera ID", Type = "string", Required = true },
                        new PropertyDefinition { Name = "ExposureTime", DisplayName = "Exposure Time", Type = "double", DefaultValue = 10000.0 }
                    }
                },

                // Inspection
                new NodeTemplate
                {
                    Type = "Inspection",
                    DisplayName = "AOI Inspection",
                    Category = "Inspection",
                    Color = "#9C27B0",
                    Inputs = new List<PortDefinition>
                    {
                        new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" },
                        new PortDefinition { Name = "Image", DisplayName = "Image", Type = "data" }
                    },
                    Outputs = new List<PortDefinition>
                    {
                        new PortDefinition { Name = "OK", DisplayName = "OK", Type = "flow" },
                        new PortDefinition { Name = "NG", DisplayName = "NG", Type = "flow" }
                    },
                    Properties = new List<PropertyDefinition>
                    {
                        new PropertyDefinition { Name = "RecipeName", DisplayName = "Recipe Name", Type = "string", Required = true }
                    }
                },

                // Laser
                new NodeTemplate
                {
                    Type = "Laser",
                    DisplayName = "Laser Measurement",
                    Category = "Laser",
                    Color = "#673AB7",
                    Inputs = new List<PortDefinition> { new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" } },
                    Outputs = new List<PortDefinition>
                    {
                        new PortDefinition { Name = "Out", DisplayName = "Out", Type = "flow" },
                        new PortDefinition { Name = "Height", DisplayName = "Height", Type = "data" }
                    }
                },

                // IO
                new NodeTemplate
                {
                    Type = "IoOutput",
                    DisplayName = "IO Output",
                    Category = "IO",
                    Color = "#607D8B",
                    Inputs = new List<PortDefinition> { new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" } },
                    Outputs = new List<PortDefinition> { new PortDefinition { Name = "Out", DisplayName = "Out", Type = "flow" } },
                    Properties = new List<PropertyDefinition>
                    {
                        new PropertyDefinition { Name = "Port", DisplayName = "Port", Type = "int", Required = true },
                        new PropertyDefinition { Name = "Value", DisplayName = "Value", Type = "bool", DefaultValue = true }
                    }
                },

                // Logic control
                new NodeTemplate
                {
                    Type = "Delay",
                    DisplayName = "Delay",
                    Category = "Logic",
                    Color = "#795548",
                    Inputs = new List<PortDefinition> { new PortDefinition { Name = "In", DisplayName = "In", Type = "flow" } },
                    Outputs = new List<PortDefinition> { new PortDefinition { Name = "Out", DisplayName = "Out", Type = "flow" } },
                    Properties = new List<PropertyDefinition>
                    {
                        new PropertyDefinition { Name = "Duration", DisplayName = "Duration (ms)", Type = "int", Required = true, DefaultValue = 1000 }
                    }
                }
            };
        }
    }
}
