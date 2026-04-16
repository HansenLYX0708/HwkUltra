using System.Text.Json.Serialization;
using HWKUltra.Tray.Implementations;
using static HWKUltra.Tray.Implementations.TrayController;

namespace HWKUltra.Tray
{
    [JsonSerializable(typeof(TrayControllerConfig))]
    [JsonSerializable(typeof(TrayConfig))]
    [JsonSerializable(typeof(PocketDataWrapper))]
    [JsonSerializable(typeof(Dictionary<string, double>))]
    [JsonSerializable(typeof(Dictionary<string, double>[]))]
    public partial class TrayJsonContext : JsonSerializerContext
    {
    }
}
