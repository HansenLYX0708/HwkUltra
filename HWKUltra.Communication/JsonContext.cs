using System.Text.Json.Serialization;
using HWKUltra.Communication.Implementations.WDConnect;

namespace HWKUltra.Communication
{
    [JsonSerializable(typeof(WDConnectCommunicationControllerConfig))]
    public partial class CommunicationJsonContext : JsonSerializerContext
    {
    }
}
