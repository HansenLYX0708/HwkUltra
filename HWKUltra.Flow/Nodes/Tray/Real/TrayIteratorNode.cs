using HWKUltra.Tray.Core;
using HWKUltra.Tray.Abstractions;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray iterator node - iterates through tray slots filtered by state.
    /// Each execution advances to the next matching slot.
    /// Outputs BranchLabel "Next" while there are more slots, "Done" when finished.
    /// Connect "Next" back to the test sequence, "Done" to the exit path.
    /// </summary>
    public class TrayIteratorNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray Iterator";
        public override string NodeType => "TrayIterator";
        protected override int SimulatedDelayMs => 10;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Tray Name", Type = "string", Required = true, Description = "Tray instance name" },
            new FlowParameter { Name = "FilterState", DisplayName = "Filter State", Type = "int", Required = false, DefaultValue = -1, Description = "Only iterate slots with this state (-1 = all, 0=Empty, 1=Present, 2=Pass, 3=Fail)" },
            new FlowParameter { Name = "Reset", DisplayName = "Reset Iterator", Type = "bool", Required = false, DefaultValue = false, Description = "Set to true to restart iteration from beginning" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "Row", DisplayName = "Current Row", Type = "int", Description = "Current slot row (0-based)" },
            new FlowParameter { Name = "Col", DisplayName = "Current Col", Type = "int", Description = "Current slot column (0-based)" },
            new FlowParameter { Name = "SlotIndex", DisplayName = "Slot Index", Type = "int", Description = "Current slot linear index" },
            new FlowParameter { Name = "SlotState", DisplayName = "Slot State", Type = "int", Description = "Current slot state" },
            new FlowParameter { Name = "TotalMatching", DisplayName = "Total Matching", Type = "int", Description = "Total number of matching slots" },
            new FlowParameter { Name = "CurrentIndex", DisplayName = "Current Index", Type = "int", Description = "Index within matching slots (0-based)" },
            new FlowParameter { Name = "HasMore", DisplayName = "Has More", Type = "bool", Description = "Whether there are more matching slots" }
        };

        public TrayIteratorNode(TrayRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                var filterState = context.GetNodeInput<int>(Id, "FilterState");
                var resetStr = context.GetNodeInput<string>(Id, "Reset");
                var reset = resetStr == "true" || resetStr == "True";

                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                // Use a context key to track iteration state
                var iterKey = $"_TrayIterator_{Id}";

                // Get or create the list of matching slots
                List<(int row, int col, int state)>? matchingSlots = null;
                int currentIdx = 0;

                if (!reset && context.Variables.TryGetValue(iterKey + "_slots", out var slotsObj))
                {
                    matchingSlots = slotsObj as List<(int row, int col, int state)>;
                    currentIdx = context.GetVariable<int>(iterKey + "_idx");
                }

                if (matchingSlots == null || reset)
                {
                    // Build the list of matching slots
                    var info = Service!.GetTrayInfo(name);
                    matchingSlots = new List<(int row, int col, int state)>();

                    for (int r = 0; r < info.Rows; r++)
                    {
                        for (int c = 0; c < info.Cols; c++)
                        {
                            var state = (int)Service.GetSlotState(name, r, c);
                            if (filterState < 0 || state == filterState)
                            {
                                matchingSlots.Add((r, c, state));
                            }
                        }
                    }

                    currentIdx = 0;
                    context.SetVariable(iterKey + "_slots", matchingSlots);
                }

                if (currentIdx >= matchingSlots.Count)
                {
                    // Iteration complete
                    context.SetNodeOutput(Id, "TotalMatching", matchingSlots.Count);
                    context.SetNodeOutput(Id, "CurrentIndex", currentIdx);
                    context.SetNodeOutput(Id, "HasMore", false);

                    // Clean up iteration state
                    context.Variables.Remove(iterKey + "_slots");
                    context.Variables.Remove(iterKey + "_idx");

                    Console.WriteLine($"[TrayIterator] {name}: Done ({matchingSlots.Count} slots iterated)");
                    return FlowResult.OkBranch("Done");
                }

                // Output current slot
                var current = matchingSlots[currentIdx];
                context.SetNodeOutput(Id, "Row", current.row);
                context.SetNodeOutput(Id, "Col", current.col);
                context.SetNodeOutput(Id, "SlotIndex", current.row * 100 + current.col);
                context.SetNodeOutput(Id, "SlotState", current.state);
                context.SetNodeOutput(Id, "TotalMatching", matchingSlots.Count);
                context.SetNodeOutput(Id, "CurrentIndex", currentIdx);
                context.SetNodeOutput(Id, "HasMore", currentIdx + 1 < matchingSlots.Count);

                // Advance index for next call
                context.SetVariable(iterKey + "_idx", currentIdx + 1);

                Console.WriteLine($"[TrayIterator] {name}: Slot [{current.row},{current.col}] ({currentIdx + 1}/{matchingSlots.Count})");
                return FlowResult.OkBranch("Next");
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"TrayIterator failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "SimTray";
            var resetStr = context.GetNodeInput<string>(Id, "Reset");
            var reset = resetStr == "true" || resetStr == "True";
            var filterState = context.GetNodeInput<int>(Id, "FilterState");

            await Task.Delay(SimulatedDelayMs, context.CancellationToken);

            // Simulate a 4x4 tray
            var iterKey = $"_TrayIterator_{Id}";
            int currentIdx = 0;

            if (!reset && context.Variables.TryGetValue(iterKey + "_idx", out var idxObj) && idxObj is int idx)
            {
                currentIdx = idx;
            }

            int totalSlots = 16; // 4x4 simulated
            if (currentIdx >= totalSlots)
            {
                context.SetNodeOutput(Id, "TotalMatching", totalSlots);
                context.SetNodeOutput(Id, "CurrentIndex", currentIdx);
                context.SetNodeOutput(Id, "HasMore", false);
                context.Variables.Remove(iterKey + "_idx");

                Console.WriteLine($"[SIMULATION] TrayIterator: {name} Done ({totalSlots} slots)");
                return FlowResult.OkBranch("Done");
            }

            int row = currentIdx / 4;
            int col = currentIdx % 4;
            context.SetNodeOutput(Id, "Row", row);
            context.SetNodeOutput(Id, "Col", col);
            context.SetNodeOutput(Id, "SlotIndex", row * 100 + col);
            context.SetNodeOutput(Id, "SlotState", 1); // Present
            context.SetNodeOutput(Id, "TotalMatching", totalSlots);
            context.SetNodeOutput(Id, "CurrentIndex", currentIdx);
            context.SetNodeOutput(Id, "HasMore", currentIdx + 1 < totalSlots);

            context.SetVariable(iterKey + "_idx", currentIdx + 1);

            Console.WriteLine($"[SIMULATION] TrayIterator: {name} [{row},{col}] ({currentIdx + 1}/{totalSlots})");
            return FlowResult.OkBranch("Next");
        }
    }
}
