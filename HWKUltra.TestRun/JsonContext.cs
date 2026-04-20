using System.Text.Json.Serialization;
using HWKUltra.Core;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.TestRun
{
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(TestSession))]
    [JsonSerializable(typeof(BoundingBox))]
    [JsonSerializable(typeof(DefectDetail))]
    [JsonSerializable(typeof(List<DefectDetail>))]
    [JsonSerializable(typeof(DetectionSummary))]
    [JsonSerializable(typeof(TrayAoiReport))]
    public partial class TestRunJsonContext : JsonSerializerContext
    {
    }
}
