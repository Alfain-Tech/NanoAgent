using System.Text.Json;
using FinalAgent.Application.Abstractions;

namespace FinalAgent.Infrastructure.Storage;

internal sealed class JsonApiKeySecretStore : IApiKeySecretStore
{
    private readonly IUserDataPathProvider _pathProvider;

    public JsonApiKeySecretStore(IUserDataPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task<string?> LoadAsync(CancellationToken cancellationToken)
    {
        string filePath = _pathProvider.GetSecretFilePath();
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using FileStream stream = File.OpenRead(filePath);
        StoredApiKeySecret? secret = await JsonSerializer.DeserializeAsync(
            stream,
            OnboardingStorageJsonContext.Default.StoredApiKeySecret,
            cancellationToken);

        return string.IsNullOrWhiteSpace(secret?.ApiKey)
            ? null
            : secret.ApiKey;
    }

    public async Task SaveAsync(string apiKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        string filePath = _pathProvider.GetSecretFilePath();
        string directoryPath = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("Secret path does not contain a parent directory.");

        FilePermissionHelper.EnsurePrivateDirectory(directoryPath);

        StoredApiKeySecret payload = new(apiKey.Trim());

        await using FileStream stream = new(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.Asynchronous);

        await JsonSerializer.SerializeAsync(
            stream,
            payload,
            OnboardingStorageJsonContext.Default.StoredApiKeySecret,
            cancellationToken);

        await stream.FlushAsync(cancellationToken);
        FilePermissionHelper.EnsurePrivateFile(filePath);
    }
}
