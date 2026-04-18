using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Abstractions;

public interface IModelProviderClient
{
    Task<IReadOnlyList<AvailableModel>> GetAvailableModelsAsync(
        AgentProviderProfile providerProfile,
        string apiKey,
        CancellationToken cancellationToken);
}
