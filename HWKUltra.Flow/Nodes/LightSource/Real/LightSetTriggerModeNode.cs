using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.LightSource.Core;

namespace HWKUltra.Flow.Nodes.LightSource.Real
{
    /// <summary>
    /// Light trigger mode node - configures light channel for external trigger mode with specified intensity.
    /// Executes the sequence: TurnOff -> SetIntensity -> SetPulseMode(External) -> TurnOn.
    /// </summary>
    public class LightSetTriggerModeNode : DeviceNodeBase<LightSourceRouter>
    {
        public override string Name { get; set; } = "Light Set Trigger Mode";
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

        protected override int SimulatedDelayMs => 80;

        public LightSetTriggerModeNode(LightSourceRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "";
                var intensity = context.GetNodeInput<int>(Id, "Intensity");

                if (string.IsNullOrEmpty(channelName))
                    return FlowResult.Fail("ChannelName is required");

                if (!Service!.HasChannel(channelName))
                    return FlowResult.Fail($"Channel '{channelName}' not found in light source configuration");

                Service.SetTriggerMode(channelName, intensity);

                context.SetNodeOutput(Id, "ChannelName", channelName);
                context.SetNodeOutput(Id, "Intensity", intensity);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Light set trigger mode failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "Unknown";
            var intensity = context.GetNodeInput<int>(Id, "Intensity");
            Console.WriteLine($"[SIMULATION] LightSetTriggerMode: {channelName} intensity={intensity}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ChannelName", channelName);
            context.SetNodeOutput(Id, "Intensity", intensity);
            return FlowResult.Ok();
        }
    }
}
