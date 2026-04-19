using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IAgentConfigurationStore
{
    Task<AgentConfiguration?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AgentConfiguration configuration, CancellationToken cancellationToken);
}
