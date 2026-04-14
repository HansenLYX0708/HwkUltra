using System.Text.Json.Serialization;
using HWKUltra.LightSource.Implementations;
using HWKUltra.LightSource.Implementations.ccs;

namespace HWKUltra.LightSource
{
    /// <summary>
    /// JSON serialization context - supports AOT compilation.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(CcsLightSourceControllerConfig))]
    [JsonSerializable(typeof(LightChannelConfig))]
    [JsonSerializable(typeof(List<LightChannelConfig>))]
    public partial class LightSourceJsonContext : JsonSerializerContext
    {
    }
}
