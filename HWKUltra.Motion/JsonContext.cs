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
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(ElmoMotionControllerConfig))]
    [JsonSerializable(typeof(GtsMotionControllerConfig))]
    [JsonSerializable(typeof(AxisConfig))]
    [JsonSerializable(typeof(GtsAxisConfig))]
    [JsonSerializable(typeof(GroupConfig))]
    [JsonSerializable(typeof(GtsGroupConfig))]
    [JsonSerializable(typeof(MotionParamConfig))]
    [JsonSerializable(typeof(CrdParamConfig))]
    [JsonSerializable(typeof(AxisMotionLimit))]
    // 集合类型
    [JsonSerializable(typeof(List<AxisConfig>))]
    [JsonSerializable(typeof(List<GtsAxisConfig>))]
    [JsonSerializable(typeof(List<GroupConfig>))]
    [JsonSerializable(typeof(List<GtsGroupConfig>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(List<CrdParamConfig>))]
    [JsonSerializable(typeof(List<short>))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(double[]))]
    public partial class MotionJsonContext : JsonSerializerContext
    {
    }
}
