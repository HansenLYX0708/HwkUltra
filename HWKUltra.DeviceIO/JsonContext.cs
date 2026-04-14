using System.Text.Json.Serialization;
using HWKUltra.DeviceIO.Implementations;
using HWKUltra.DeviceIO.Implementations.galil;

namespace HWKUltra.DeviceIO
{
    /// <summary>
    /// JSON serialization context - AOT compilation support (corresponds to MotionJsonContext).
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(GalilIOConfig))]
    [JsonSerializable(typeof(GalilCardConfig))]
    [JsonSerializable(typeof(IOPointConfig))]
    // Collection types
    [JsonSerializable(typeof(List<GalilCardConfig>))]
    [JsonSerializable(typeof(List<IOPointConfig>))]
    [JsonSerializable(typeof(List<string>))]
    public partial class IOJsonContext : JsonSerializerContext
    {
    }
}
