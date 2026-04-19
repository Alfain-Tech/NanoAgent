using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;

namespace FinalAgent.Domain.Services;

internal sealed class AgentProviderProfileFactory : IAgentProviderProfileFactory
{
    public AgentProviderProfile CreateOpenAi()
    {
        return new AgentProviderProfile(ProviderKind.OpenAi, BaseUrl: null);
    }

    public AgentProviderProfile CreateCompatible(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        string normalizedBaseUrl = CompatibleProviderBaseUrlNormalizer.Normalize(baseUrl);
        return new AgentProviderProfile(ProviderKind.OpenAiCompatible, normalizedBaseUrl);
    }
}
