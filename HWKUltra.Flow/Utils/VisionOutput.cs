using HWKUltra.Flow.Abstractions;

namespace HWKUltra.Flow.Utils
{
    /// <summary>
    /// Helper that mirrors a vision-node result into the shared <see cref="FlowContext.Variables"/>
    /// under a user-chosen name (supplied via an <c>OutputVariable</c> input parameter).
    /// Keeps all Vision nodes consistent: scalar primary output lives at the given name;
    /// optional additional members live at <c>{name}_Suffix</c>.
    /// </summary>
    public static class VisionOutput
    {
        /// <summary>
        /// Publish a primary scalar/array result to <c>context.Variables[outputVar]</c>
        /// if the node's <paramref name="inputName"/> property is set to a non-empty value.
        /// </summary>
        public static void Publish(FlowContext context, string nodeId, string inputName, object? value)
        {
            var outputVar = context.GetNodeInput<string>(nodeId, inputName);
            if (string.IsNullOrWhiteSpace(outputVar) || value == null) return;
            context.Variables[outputVar!] = value;
        }

        /// <summary>
        /// Publish multiple named components (e.g. X/Y/Width/Height) as
        /// <c>{outputVar}_Key</c>. Primary value is written at <c>{outputVar}</c>.
        /// </summary>
        public static void PublishCompound(FlowContext context, string nodeId, string inputName,
            object? primary, params (string Key, object? Value)[] components)
        {
            var outputVar = context.GetNodeInput<string>(nodeId, inputName);
            if (string.IsNullOrWhiteSpace(outputVar)) return;
            if (primary != null) context.Variables[outputVar!] = primary;
            foreach (var (k, v) in components)
            {
                if (v == null) continue;
                context.Variables[$"{outputVar}_{k}"] = v;
            }
        }
    }
}
