using NanoAgent.Application.Models;
using System.Text.Json.Serialization;
using NanoAgent.Domain.Models;

namespace NanoAgent.Infrastructure.Storage;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true)]
[JsonSerializable(typeof(AgentConfiguration))]
[JsonSerializable(typeof(AgentProviderProfile))]
internal sealed partial class OnboardingStorageJsonContext : JsonSerializerContext
{
}
