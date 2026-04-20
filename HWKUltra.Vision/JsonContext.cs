using System.Collections.Generic;
using System.Text.Json.Serialization;
using HWKUltra.Vision.Abstractions;

namespace HWKUltra.Vision
{
    /// <summary>
    /// JSON source-generation context for Vision types (AOT-safe).
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(VisionDetection))]
    [JsonSerializable(typeof(List<VisionDetection>))]
    [JsonSerializable(typeof(VisionColor))]
    [JsonSerializable(typeof(Dictionary<string, VisionColor>))]
    public partial class VisionJsonContext : JsonSerializerContext
    {
    }
}
