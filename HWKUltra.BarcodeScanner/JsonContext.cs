using System.Text.Json.Serialization;
using HWKUltra.BarcodeScanner.Implementations;

namespace HWKUltra.BarcodeScanner
{
    [JsonSerializable(typeof(SerialBarcodeScannerControllerConfig))]
    [JsonSerializable(typeof(BarcodeScannerConfig))]
    public partial class BarcodeScannerJsonContext : JsonSerializerContext
    {
    }
}
