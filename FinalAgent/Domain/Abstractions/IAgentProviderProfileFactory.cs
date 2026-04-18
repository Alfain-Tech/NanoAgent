using FinalAgent.Domain.Models;

namespace FinalAgent.Domain.Abstractions;

public interface IAgentProviderProfileFactory
{
    AgentProviderProfile CreateOpenAi();

    AgentProviderProfile CreateCompatible(string baseUrl);
}
