using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;
using HWKUltra.LightSource.Core;

namespace HWKUltra.Flow.Nodes.LightSource.Real
{
    /// <summary>
    /// Light on/off node - turns a light channel on or off.
    /// </summary>
    public class LightTurnOnOffNode : DeviceNodeBase<LightSourceRouter>
    {
        public override string Name { get; set; } = "Light Turn On/Off";
        public override string NodeType => "LightTurnOnOff";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "ChannelName", DisplayName = "Channel Name", Type = "string", Required = true, Description = "Light channel name (e.g., TopLight, BottomLight)" },
            new FlowParameter { Name = "Value", DisplayName = "Value", Type = "bool", Required = true, DefaultValue = true, Description = "true=ON, false=OFF" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "ChannelName", DisplayName = "Channel Name", Type = "string", Description = "Channel that was operated" },
            new FlowParameter { Name = "ActualValue", DisplayName = "Actual Value", Type = "bool", Description = "Actual on/off state" }
        };

        protected override int SimulatedDelayMs => 50;

        public LightTurnOnOffNode(LightSourceRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "";
                var value = context.GetNodeInput<string>(Id, "Value") != "false";

                if (string.IsNullOrEmpty(channelName))
                    return FlowResult.Fail("ChannelName is required");

                if (!Service!.HasChannel(channelName))
                    return FlowResult.Fail($"Channel '{channelName}' not found in light source configuration");

                if (value)
                    Service.TurnOn(channelName);
                else
                    Service.TurnOff(channelName);

                context.SetNodeOutput(Id, "ChannelName", channelName);
                context.SetNodeOutput(Id, "ActualValue", value);

                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Light turn on/off failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "Unknown";
            var value = context.GetNodeInput<string>(Id, "Value") != "false";
            Console.WriteLine($"[SIMULATION] LightTurnOnOff: {channelName} = {(value ? "ON" : "OFF")}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            context.SetNodeOutput(Id, "ChannelName", channelName);
            context.SetNodeOutput(Id, "ActualValue", value);
            return FlowResult.Ok();
        }
    }
}
