using HWKUltra.Tray.Core;
using HWKUltra.Tray.Abstractions;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Tray.Real
{
    /// <summary>
    /// Tray teach node - teach pocket positions using 4-corner interpolation.
    /// </summary>
    public class TrayTeachNode : DeviceNodeBase<TrayRouter>
    {
        public override string Name { get; set; } = "Tray Teach";
        public override string NodeType => "TrayTeach";
        protected override int SimulatedDelayMs => 100;

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "InstanceName", DisplayName = "Instance Name", Type = "string", Required = true, Description = "Tray instance name" },
            new FlowParameter { Name = "LT_X", DisplayName = "LeftTop X", Type = "double", Required = true },
            new FlowParameter { Name = "LT_Y", DisplayName = "LeftTop Y", Type = "double", Required = true },
            new FlowParameter { Name = "LT_Z", DisplayName = "LeftTop Z", Type = "double", Required = true },
            new FlowParameter { Name = "RT_X", DisplayName = "RightTop X", Type = "double", Required = true },
            new FlowParameter { Name = "RT_Y", DisplayName = "RightTop Y", Type = "double", Required = true },
            new FlowParameter { Name = "RT_Z", DisplayName = "RightTop Z", Type = "double", Required = true },
            new FlowParameter { Name = "LB_X", DisplayName = "LeftBottom X", Type = "double", Required = true },
            new FlowParameter { Name = "LB_Y", DisplayName = "LeftBottom Y", Type = "double", Required = true },
            new FlowParameter { Name = "LB_Z", DisplayName = "LeftBottom Z", Type = "double", Required = true },
            new FlowParameter { Name = "RB_X", DisplayName = "RightBottom X", Type = "double", Required = true },
            new FlowParameter { Name = "RB_Y", DisplayName = "RightBottom Y", Type = "double", Required = true },
            new FlowParameter { Name = "RB_Z", DisplayName = "RightBottom Z", Type = "double", Required = true }
        };

        public override List<FlowParameter> Outputs { get; } = new();

        public TrayTeachNode(TrayRouter? router = null) : base(router) { }

        protected override async Task<FlowResult> ExecuteRealAsync(FlowContext context)
        {
            await Task.CompletedTask;
            try
            {
                var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "";
                if (string.IsNullOrEmpty(name))
                    return FlowResult.Fail("InstanceName is required");

                var lt = new Point3D(
                    double.Parse(context.GetNodeInput<string>(Id, "LT_X") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "LT_Y") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "LT_Z") ?? "0"));
                var rt = new Point3D(
                    double.Parse(context.GetNodeInput<string>(Id, "RT_X") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "RT_Y") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "RT_Z") ?? "0"));
                var lb = new Point3D(
                    double.Parse(context.GetNodeInput<string>(Id, "LB_X") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "LB_Y") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "LB_Z") ?? "0"));
                var rb = new Point3D(
                    double.Parse(context.GetNodeInput<string>(Id, "RB_X") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "RB_Y") ?? "0"),
                    double.Parse(context.GetNodeInput<string>(Id, "RB_Z") ?? "0"));

                Service!.InitPositions(name, lt, rt, lb, rb);
                return FlowResult.Ok();
            }
            catch (Exception ex)
            {
                return FlowResult.Fail($"Tray teach failed: {ex.Message}");
            }
        }

        protected override async Task<FlowResult> ExecuteSimulatedAsync(FlowContext context)
        {
            var name = context.GetNodeInput<string>(Id, "InstanceName") ?? "Unknown";
            Console.WriteLine($"[SIMULATION] TrayTeach: 4-corner teach for {name}");
            await Task.Delay(SimulatedDelayMs, context.CancellationToken);
            return FlowResult.Ok();
        }
    }
}
