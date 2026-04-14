using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.LightSource.Core;

namespace HWKUltra.Flow.Nodes.LightSource.Real
{
    /// <summary>
    /// Light continuous mode node - configures light channel for continuous lighting.
    /// Executes the sequence: TurnOff -> SetIntensity(low) -> SetPulseMode(Off) -> TurnOn.
    /// </summary>
    public class LightSetContinuousModeNode : DeviceNodeBase<LightSourceRouter>
    {
        public override string Name { get; set; } = "Light Set Continuous Mode";
        public override string NodeType => "LightSetContinuousMode";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "ChannelName", DisplayName = "Channel Name", Type = "string", Required = true, Description = "Light channel name (e.g., TopLight, BottomLight)" },
            new FlowParameter { Name = "Intensity", DisplayName = "Intensity", Type = "int", Required = false, DefaultValue = 1, Description = "Intensity for continuous mode (default: 1)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ChannelName", DisplayName = "Channel Name", Type = "string", Description = "Channel that was configured" },
            new FlowParameter { Name = "Intensity", DisplayName = "Intensity", Type = "int", Description = "Intensity that was set" }
        };

        protected override int SimulatedDelayMs => 120;

        public LightSetContinuousModeNode(LightSourceRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "";
                var intensity = context.GetNodeInput<int>(Id, "Intensity");
                if (intensity <= 0) intensity = 1;

                if (string.IsNullOrEmpty(channelName))
                    return FlowResult.Fail("ChannelName is required");

                if (!Service!.HasChannel(channelName))
                    return FlowResult.Fail($"Channel '{channelName}' not found in light source configuration");

                Service.SetContinuousMode(channelName, intensity);

                context.SetNodeOutput(Id, "ChannelName", channelName);
                context.SetNodeOutput(Id, "Intensity", intensity);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Light set continuous mode failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "Unknown";
            var intensity = context.GetNodeInput<int>(Id, "Intensity");
            if (intensity <= 0) intensity = 1;
            Console.WriteLine($"[SIMULATION] LightSetContinuousMode: {channelName} intensity={intensity}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ChannelName", channelName);
            context.SetNodeOutput(Id, "Intensity", intensity);
            return FlowResult.Ok();
        }
    }
}
