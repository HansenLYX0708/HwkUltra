using System.Text.Json.Serialization;
using HWKUltra.AutoFocus.Implementations;
using HWKUltra.AutoFocus.Implementations.laf;

namespace HWKUltra.AutoFocus
{
    /// <summary>
    /// JSON source generation context for AutoFocus configuration (AOT compatible).
    /// </summary>
    [JsonSerializable(typeof(LafAutoFocusControllerConfig))]
    [JsonSerializable(typeof(AutoFocusConfig))]
    [JsonSerializable(typeof(List<AutoFocusConfig>))]
    public partial class AutoFocusJsonContext : JsonSerializerContext
    {
    }
}
