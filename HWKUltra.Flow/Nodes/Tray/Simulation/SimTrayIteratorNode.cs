using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Simulation
{
    /// <summary>
    /// Simulation tray iterator node - iterates through simulated tray slots.
    /// </summary>
    public class SimTrayIteratorNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Sim Tray Iterator";
        public override string NodeType => "TrayIterator";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Tray Name", Type = "string", Required = true },
            new FlowParameter { Name = "FilterState", DisplayName = "Filter State", Type = "int", Required = false, DefaultValue = -1 },
            new FlowParameter { Name = "Reset", DisplayName = "Reset Iterator", Type = "bool", Required = false, DefaultValue = false }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Row", DisplayName = "Current Row", Type = "int" },
            new FlowParameter { Name = "Col", DisplayName = "Current Col", Type = "int" },
            new FlowParameter { Name = "SlotIndex", DisplayName = "Slot Index", Type = "int" },
            new FlowParameter { Name = "SlotState", DisplayName = "Slot State", Type = "int" },
            new FlowParameter { Name = "TotalMatching", DisplayName = "Total Matching", Type = "int" },
            new FlowParameter { Name = "CurrentIndex", DisplayName = "Current Index", Type = "int" },
            new FlowParameter { Name = "HasMore", DisplayName = "Has More", Type = "bool" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "SimTray";
            var resetStr = context.GetNodeInput<string>(Id, "Reset");
            var reset = resetStr == "true" || resetStr == "True";

            await Task.Delay(10, context.CancellationToken);

            var iterKey = $"_TrayIterator_{Id}";
            int currentIdx = 0;

            if (!reset && context.Variables.TryGetValue(iterKey + "_idx", out var idxObj) && idxObj is int idx)
            {
                currentIdx = idx;
            }

            int totalSlots = 16;
            if (currentIdx >= totalSlots)
            {
                context.SetNodeOutput(Id, "TotalMatching", totalSlots);
                context.SetNodeOutput(Id, "CurrentIndex", currentIdx);
                context.SetNodeOutput(Id, "HasMore", false);
                context.Variables.Remove(iterKey + "_idx");

                Console.WriteLine($"[SIMULATION] TrayIterator: {name} Done");
                return FlowResult.OkBranch("Done");
            }

            int row = currentIdx / 4;
            int col = currentIdx % 4;
            context.SetNodeOutput(Id, "Row", row);
            context.SetNodeOutput(Id, "Col", col);
            context.SetNodeOutput(Id, "SlotIndex", row * 100 + col);
            context.SetNodeOutput(Id, "SlotState", 1);
            context.SetNodeOutput(Id, "TotalMatching", totalSlots);
            context.SetNodeOutput(Id, "CurrentIndex", currentIdx);
            context.SetNodeOutput(Id, "HasMore", currentIdx + 1 < totalSlots);

            context.SetVariable(iterKey + "_idx", currentIdx + 1);

            Console.WriteLine($"[SIMULATION] TrayIterator: {name} [{row},{col}] ({currentIdx + 1}/{totalSlots})");
            return FlowResult.OkBranch("Next");
        }
    }
}
