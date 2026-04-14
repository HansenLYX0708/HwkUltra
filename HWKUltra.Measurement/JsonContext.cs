using System.Text.Json.Serialization;
using HWKUltra.Measurement.Implementations;
using HWKUltra.Measurement.Implementations.Keyence;

namespace HWKUltra.Measurement
{
    [JsonSerializable(typeof(KeyenceMeasurementControllerConfig))]
    [JsonSerializable(typeof(MeasurementConfig))]
    [JsonSerializable(typeof(List<MeasurementConfig>))]
    public partial class MeasurementJsonContext : JsonSerializerContext
    {
    }
}
