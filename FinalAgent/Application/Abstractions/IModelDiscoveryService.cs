using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IModelDiscoveryService
{
    Task<ModelDiscoveryResult> DiscoverAndSelectAsync(CancellationToken cancellationToken);
}
