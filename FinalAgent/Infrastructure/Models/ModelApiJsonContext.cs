using System.Text.Json.Serialization;

namespace FinalAgent.Infrastructure.Models;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
[JsonSerializable(typeof(ModelListResponse))]
internal sealed partial class ModelApiJsonContext : JsonSerializerContext
{
}
