using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Models;

public sealed record AgentConfiguration(
    AgentProviderProfile ProviderProfile,
    string? PreferredModelId);
