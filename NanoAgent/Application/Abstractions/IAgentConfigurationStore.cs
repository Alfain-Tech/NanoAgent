using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IAgentConfigurationStore
{
    Task<AgentConfiguration?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AgentConfiguration configuration, CancellationToken cancellationToken);
}
