using HWKUltra.Core;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.TeachData.Real
{
    /// <summary>
    /// Updates a teach position with new axis values and optionally saves to file.
    /// Useful for runtime teach operations.
    /// </summary>
    public class SetTeachPositionNode : LogicNodeBase
    {
        private readonly TeachDataService? _teachData;

        public override string Name { get; set; } = "Set Teach Position";
        public override string NodeType => "SetTeachPosition";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "PositionName", DisplayName = "Position Name", Type = "string", Required = true, Description = "Teach position name" },
            new FlowParameter { Name = "Group", DisplayName = "Group", Type = "string", Required = false, DefaultValue = "Custom", Description = "Group name" },
            new FlowParameter { Name = "X", DisplayName = "X", Type = "double", Required = false, DefaultValue = 0.0, Description = "X axis value" },
            new FlowParameter { Name = "Y", DisplayName = "Y", Type = "double", Required = false, DefaultValue = 0.0, Description = "Y axis value" },
            new FlowParameter { Name = "Z", DisplayName = "Z", Type = "double", Required = false, DefaultValue = 0.0, Description = "Z axis value" },
            new FlowParameter { Name = "AutoSave", DisplayName = "Auto Save", Type = "bool", Required = false, DefaultValue = false, Description = "Save to file immediately" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Success", DisplayName = "Success", Type = "bool", Description = "Whether the position was set successfully" }
        };

        public SetTeachPositionNode(TeachDataService? teachData)
        {
            _teachData = teachData;
        }

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            if (_teachData == null)
            {
                context.SetNodeOutput(Id, "Success", false);
                return Task.FromResult(FlowResult.Fail("TeachDataService not available"));
            }

            try
            {
                var posName = context.GetNodeInput<string>(Id, "PositionName")
                    ?? throw new ArgumentException("PositionName is required");
                var group = context.GetNodeInput<string>(Id, "Group") ?? "Custom";
                var x = context.GetNodeInput<double>(Id, "X");
                var y = context.GetNodeInput<double>(Id, "Y");
                var z = context.GetNodeInput<double>(Id, "Z");
                var autoSave = context.GetNodeInput<string>(Id, "AutoSave") == "true";

                var pos = Pos.XYZ(x, y, z);
                _teachData.SetPosition(posName, group, pos);

                if (autoSave)
                    _teachData.Save();

                Console.WriteLine($"[TeachData] Set position '{posName}' [{group}]: {pos}");
                context.SetNodeOutput(Id, "Success", true);
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                context.SetNodeOutput(Id, "Success", false);
                return Task.FromResult(FlowResult.Fail($"SetTeachPosition failed: {ex.Message}"));
            }
        }
    }
}
