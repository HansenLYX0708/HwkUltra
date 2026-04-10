using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HWKUltra.Motion.Implementations;
using HWKUltra.Motion.Implementations.elmo;
using HWKUltra.Motion.Implementations.gts;

namespace HWKUltra.Motion
{
    /// <summary>
    /// JSON序列化上下文 - 支持AOT编译
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ElmoMotionControllerConfig))]
    [JsonSerializable(typeof(GtsMotionControllerConfig))]
    [JsonSerializable(typeof(AxisConfig))]
    [JsonSerializable(typeof(GtsAxisConfig))]
    [JsonSerializable(typeof(GroupConfig))]
    [JsonSerializable(typeof(GtsGroupConfig))]
    [JsonSerializable(typeof(MotionParamConfig))]
    [JsonSerializable(typeof(CrdParamConfig))]
    [JsonSerializable(typeof(AxisMotionLimit))]
    [JsonSerializable(typeof(JsonDocument))]
    [JsonSerializable(typeof(JsonNode))]
    public partial class MotionJsonContext : JsonSerializerContext
    {
    }
}
