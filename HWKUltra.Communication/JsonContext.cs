using System.Text.Json.Serialization;
using HWKUltra.Communication.Implementations.GenericPlc;
using HWKUltra.Communication.Implementations.WDConnect;

namespace HWKUltra.Communication
{
    [JsonSerializable(typeof(WDConnectCommunicationControllerConfig))]
    [JsonSerializable(typeof(GenericPlcConfig))]
    [JsonSerializable(typeof(PlcCommandDef))]
    public partial class CommunicationJsonContext : JsonSerializerContext
    {
    }
}
