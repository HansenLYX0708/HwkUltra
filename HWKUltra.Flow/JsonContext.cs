using System.Text.Json;
using System.Text.Json.Serialization;
using HWKUltra.Flow.Models;

namespace HWKUltra.Flow
{
    /// <summary>
    /// JSON serialization context - supports AOT compilation
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(FlowDefinition))]
    [JsonSerializable(typeof(NodeDefinition))]
    [JsonSerializable(typeof(ConnectionDefinition))]
    [JsonSerializable(typeof(List<FlowDefinition>))]
    [JsonSerializable(typeof(List<NodeDefinition>))]
    [JsonSerializable(typeof(List<ConnectionDefinition>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    public partial class FlowJsonContext : JsonSerializerContext
    {
    }
}
