using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Abstractions;

public interface IAgentConfigurationStore
{
    Task<AgentProviderProfile?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AgentProviderProfile configuration, CancellationToken cancellationToken);
}
