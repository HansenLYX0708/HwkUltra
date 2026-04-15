using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Models;
using HWKUltra.Creator.Models;

namespace HWKUltra.Creator.ViewModels
{
    /// <summary>
    /// ViewModel for a single flow node on the canvas
    /// </summary>
    public partial class FlowNodeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _nodeType = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private double _x;

        [ObservableProperty]
        private double _y;

        [ObservableProperty]
        private double _width = 160;

        [ObservableProperty]
        private double _height = 80;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _color = "#2196F3";

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isStartNode;

        /// <summary>
        /// Node properties (key-value configuration parameters)
        /// </summary>
        public ObservableCollection<NodePropertyViewModel> Properties { get; } = new();

        /// <summary>
        /// Input parameter definitions
        /// </summary>
        public ObservableCollection<FlowParameter> InputDefinitions { get; } = new();

        /// <summary>
        /// Output parameter definitions
        /// </summary>
        public ObservableCollection<FlowParameter> OutputDefinitions { get; } = new();

        /// <summary>
        /// Create from NodeDefinition + catalog entry
        /// </summary>
        public static FlowNodeViewModel FromDefinition(NodeDefinition def, NodeCatalogEntry? catalogEntry)
        {
            var vm = new FlowNodeViewModel
            {
                Id = def.Id,
                Name = def.Name,
                NodeType = def.Type,
                Description = def.Description,
                X = def.X,
                Y = def.Y,
                Category = catalogEntry?.Category ?? "Unknown",
                Color = catalogEntry?.Color ?? "#2196F3"
            };

            // Populate input/output definitions from catalog
            if (catalogEntry != null)
            {
                foreach (var input in catalogEntry.InputDefinitions)
                    vm.InputDefinitions.Add(input);
                foreach (var output in catalogEntry.OutputDefinitions)
                    vm.OutputDefinitions.Add(output);
            }

            // Populate properties from definition
            foreach (var kvp in def.Properties)
            {
                vm.Properties.Add(new NodePropertyViewModel
                {
                    Name = kvp.Key,
                    Value = kvp.Value,
                    ParameterDef = vm.InputDefinitions.FirstOrDefault(i => i.Name == kvp.Key)
                });
            }

            // Add missing properties with defaults from input definitions
            if (catalogEntry != null)
            {
                foreach (var input in catalogEntry.InputDefinitions)
                {
                    if (!def.Properties.ContainsKey(input.Name))
                    {
                        vm.Properties.Add(new NodePropertyViewModel
                        {
                            Name = input.Name,
                            Value = input.DefaultValue?.ToString() ?? string.Empty,
                            ParameterDef = input
                        });
                    }
                }
            }

            return vm;
        }

        /// <summary>
        /// Convert back to NodeDefinition for serialization
        /// </summary>
        public NodeDefinition ToDefinition()
        {
            var def = new NodeDefinition
            {
                Id = Id,
                Type = NodeType,
                Name = Name,
                Description = Description,
                X = X,
                Y = Y
            };

            foreach (var prop in Properties)
            {
                def.Properties[prop.Name] = prop.Value;
            }

            return def;
        }
    }

    /// <summary>
    /// ViewModel for a single node property (key-value pair with metadata)
    /// </summary>
    public partial class NodePropertyViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        /// <summary>
        /// Associated parameter definition (for type info, display name, etc.)
        /// </summary>
        public FlowParameter? ParameterDef { get; set; }

        public string DisplayName => ParameterDef?.DisplayName ?? Name;
        public string Type => ParameterDef?.Type ?? "string";
        public bool IsRequired => ParameterDef?.Required ?? false;
        public string? ParameterDescription => ParameterDef?.Description;
    }
}
