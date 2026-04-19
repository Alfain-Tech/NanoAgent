namespace NanoAgent.Application.Abstractions;

public interface IApiKeySecretStore
{
    Task<string?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(string apiKey, CancellationToken cancellationToken);
}
