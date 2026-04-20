using NanoAgent.Application.Abstractions;
using NanoAgent.Infrastructure.Configuration;

namespace NanoAgent.Infrastructure.Secrets;

internal sealed class ApiKeySecretStore : IApiKeySecretStore
{
    private const string ApiKeyAccountName = "default-api-key";

    private readonly IPlatformCredentialStore _platformCredentialStore;

    public ApiKeySecretStore(IPlatformCredentialStore platformCredentialStore)
    {
        _platformCredentialStore = platformCredentialStore;
    }

    public Task<string?> LoadAsync(CancellationToken cancellationToken)
    {
        return _platformCredentialStore.LoadAsync(BuildReference(), cancellationToken);
    }

    public Task SaveAsync(string apiKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        return _platformCredentialStore.SaveAsync(
            BuildReference(),
            apiKey.Trim(),
            cancellationToken);
    }

    private SecretReference BuildReference()
    {
        return new SecretReference(
            ApplicationIdentity.ProductName,
            ApiKeyAccountName,
            $"{ApplicationIdentity.ProductName} API key");
    }
}
