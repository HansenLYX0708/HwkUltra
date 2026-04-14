using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.LightSource.Simulation
{
    /// <summary>
    /// Simulated light on/off node - no hardware dependency.
    /// </summary>
    public class SimLightTurnOnOffNode : LogicNodeBase
    {
        public override string Name { get; set; } = "Light Turn On/Off (Sim)";
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

        public override async Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            var channelName = context.GetNodeInput<string>(Id, "ChannelName") ?? "Unknown";
            var value = context.GetNodeInput<string>(Id, "Value") != "false";
            Console.WriteLine($"[SIMULATION] LightTurnOnOff: {channelName} = {(value ? "ON" : "OFF")}");
            await Task.Delay(50, context.CancellationToken);
            context.SetNodeOutput(Id, "ChannelName", channelName);
            context.SetNodeOutput(Id, "ActualValue", value);
            return FlowResult.Ok();
        }
    }
}
