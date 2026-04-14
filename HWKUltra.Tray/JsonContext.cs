using System.Text.Json.Serialization;
using HWKUltra.Tray.Abstractions;
using HWKUltra.Tray.Implementations;
using static HWKUltra.Tray.Implementations.TrayController;

namespace HWKUltra.Tray
{
    [JsonSerializable(typeof(TrayControllerConfig))]
    [JsonSerializable(typeof(TrayConfig))]
    [JsonSerializable(typeof(PocketDataWrapper))]
    [JsonSerializable(typeof(Point3D))]
    [JsonSerializable(typeof(Point3D[]))]
    public partial class TrayJsonContext : JsonSerializerContext
    {
    }
}
