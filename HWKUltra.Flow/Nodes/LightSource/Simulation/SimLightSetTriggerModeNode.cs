using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.LightSource.Simulation
{
    /// <summary>
    /// Simulated light trigger mode node - no hardware dependency.
    /// </summary>
    public class SimLightSetTriggerModeNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Light Set Trigger Mode (Sim)";
        public override string NodeType => "LightSetTriggerMode";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "ChannelName", DisplayName = "Channel Name", Type = "string", Required = true, Description = "Light channel name (e.g., TopLight, BottomLight)" },
            new FlowParameter { Name = "Intensity", DisplayName = "Intensity", Type = "int", Required = true, DefaultValue = 512, Description = "Light intensity (0 to MaxIntensity)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ChannelName", DisplayName = "Channel Name", Type = "string", Description = "Channel that was configured" },
            new FlowParameter { Name = "Intensity", DisplayName = "Intensity", Type = "int", Description = "Intensity that was set" }
        };

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "Unknown";
            var intensity = context.GetNodeInput<int>(Id, "Intensity");
            Console.WriteLine($"[SIMULATION] LightSetTriggerMode: {channelName} intensity={intensity}");
            await Task.Delay(80, context.CancellationToken);
            context.SetNodeOutput(Id, "ChannelName", channelName);
            context.SetNodeOutput(Id, "Intensity", intensity);
            return FlowResult.Ok();
        }
    }
}
