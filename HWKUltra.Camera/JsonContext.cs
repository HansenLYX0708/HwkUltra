using System.Text.Json.Serialization;
using HWKUltra.Camera.Implementations;
using HWKUltra.Camera.Implementations.basler;

namespace HWKUltra.Camera
{
    /// <summary>
    /// JSON serialization context - supports AOT compilation.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(BaslerCameraControllerConfig))]
    [JsonSerializable(typeof(CameraConfig))]
    [JsonSerializable(typeof(List<CameraConfig>))]
    public partial class CameraJsonContext : JsonSerializerContext
    {
    }
}
