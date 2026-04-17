using System.Text.Json.Serialization;
using HWKUltra.Tray.Implementations.WDTray;
using static HWKUltra.Tray.Implementations.WDTray.TrayController;

namespace HWKUltra.Tray
{
    [JsonSerializable(typeof(TrayControllerConfig))]
    [JsonSerializable(typeof(TrayConfig))]
    [JsonSerializable(typeof(SlotStateDefinition))]
    [JsonSerializable(typeof(DefectCodeDefinition))]
    [JsonSerializable(typeof(PocketDataWrapper))]
    [JsonSerializable(typeof(Dictionary<string, double>))]
    [JsonSerializable(typeof(Dictionary<string, double>[]))]
    public partial class TrayJsonContext : JsonSerializerContext
    {
    }
}
