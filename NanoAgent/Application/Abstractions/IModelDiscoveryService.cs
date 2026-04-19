using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IModelDiscoveryService
{
    Task<ModelDiscoveryResult> DiscoverAndSelectAsync(CancellationToken cancellationToken);
}
