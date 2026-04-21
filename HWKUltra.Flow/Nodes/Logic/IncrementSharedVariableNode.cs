using System.Globalization;
using HWKUltra.Flow.Abstractions;
using HWKUltra.Flow.Nodes.Abstractions;

namespace HWKUltra.Flow.Nodes.Logic
{
    /// <summary>
    /// Atomically increment an integer shared variable and return its *previous* value.
    /// Perfect for worker-pool "claim next task" patterns: each worker reads a unique
    /// index without needing external locks.
    /// </summary>
    public class IncrementSharedVariableNode : LogicNodeBase
    {
        private static readonly object _globalGate = new();

        public override string Name { get; set; } = "Increment Shared Variable";
        public override string NodeType => "IncrementSharedVariable";

        public override List<FlowParameter> Inputs { get; } = new()
        {
            new FlowParameter { Name = "Key",            DisplayName = "Variable Key",   Type = "string", Required = true, Description = "Shared variable name (must hold an integer)" },
            new FlowParameter { Name = "Delta",          DisplayName = "Delta",          Type = "int",    Required = false, DefaultValue = 1, Description = "Amount to add (default 1)" },
            new FlowParameter { Name = "InitialValue",   DisplayName = "Initial Value",  Type = "int",    Required = false, DefaultValue = 0, Description = "Value used if the key does not yet exist" },
            new FlowParameter { Name = "TargetVariable", DisplayName = "Target Variable", Type = "string", Required = false, Description = "Local variable name to receive the previous value (default: same as Key)" }
        };

        public override List<FlowParameter> Outputs { get; } = new()
        {
            new FlowParameter { Name = "PreviousValue", DisplayName = "Previous Value", Type = "int", Description = "Value before increment (the 'claimed' index)" },
            new FlowParameter { Name = "NewValue",      DisplayName = "New Value",      Type = "int", Description = "Value after increment" }
        };

        public override Task<FlowResult> ExecuteAsync(FlowContext context)
        {
            try
            {
                var key = context.GetNodeInput<string>(Id, "Key") ?? "";
                var delta = context.GetNodeInput<int>(Id, "Delta");
                if (delta == 0) delta = 1;
                var initial = context.GetNodeInput<int>(Id, "InitialValue");
                var target = context.GetNodeInput<string>(Id, "TargetVariable");
                if (string.IsNullOrWhiteSpace(target)) target = key;

                if (string.IsNullOrEmpty(key))
                    return Task.FromResult(FlowResult.Fail("Key is required"));
                if (context.SharedContext == null)
                    return Task.FromResult(FlowResult.Fail("SharedContext is not available."));

                int prev, next;
                // A single global gate is sufficient: Increment operations are fast,
                // and we only need mutual exclusion with other IncrementSharedVariable
                // calls on the same SharedFlowContext.
                lock (_globalGate)
                {
                    prev = TryParseInt(context.SharedContext.GetVariable<object>(key), initial);
                    next = prev + delta;
                    context.SharedContext.SetVariable(key, next);
                }

                context.Variables[target!] = prev;
                context.SetNodeOutput(Id, "PreviousValue", prev);
                context.SetNodeOutput(Id, "NewValue", next);

                Console.WriteLine($"[IncrementSharedVar] {key}: {prev} -> {next} (claimed={prev} as {target})");
                return Task.FromResult(FlowResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(FlowResult.Fail($"IncrementSharedVariable failed: {ex.Message}"));
            }
        }

        private static int TryParseInt(object? v, int fallback)
        {
            if (v == null) return fallback;
            if (v is int i) return i;
            if (v is long l) return (int)l;
            if (v is double d) return (int)d;
            var s = v.ToString();
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) ? p : fallback;
        }
    }
}
