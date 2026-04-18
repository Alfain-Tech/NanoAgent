using System.Text.Json.Serialization;
using FinalAgent.Domain.Models;

namespace FinalAgent.Infrastructure.Storage;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true)]
[JsonSerializable(typeof(AgentProviderProfile))]
[JsonSerializable(typeof(StoredApiKeySecret))]
internal sealed partial class OnboardingStorageJsonContext : JsonSerializerContext
{
}
