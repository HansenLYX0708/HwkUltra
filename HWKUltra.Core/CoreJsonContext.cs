using System.Text.Json.Serialization;

namespace HWKUltra.Core
{
    [JsonSerializable(typeof(Dictionary<string, double>))]
    public partial class CoreJsonContext : JsonSerializerContext
    {
    }
}
