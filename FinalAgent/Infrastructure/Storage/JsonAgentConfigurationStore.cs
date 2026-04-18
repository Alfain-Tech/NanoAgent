using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Domain.Models;

namespace FinalAgent.Infrastructure.Storage;

internal sealed class JsonAgentConfigurationStore : IAgentConfigurationStore
{
    private readonly IUserDataPathProvider _pathProvider;

    public JsonAgentConfigurationStore(IUserDataPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task<AgentProviderProfile?> LoadAsync(CancellationToken cancellationToken)
    {
        string filePath = _pathProvider.GetConfigurationFilePath();
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using FileStream stream = File.OpenRead(filePath);
        AgentProviderProfile? profile = await JsonSerializer.DeserializeAsync(
            stream,
            OnboardingStorageJsonContext.Default.AgentProviderProfile,
            cancellationToken);

        if (profile is null)
        {
            return null;
        }

        return profile.ProviderKind switch
        {
            ProviderKind.OpenAi => new AgentProviderProfile(ProviderKind.OpenAi, BaseUrl: null),
            ProviderKind.OpenAiCompatible when !string.IsNullOrWhiteSpace(profile.BaseUrl)
                => new AgentProviderProfile(
                    ProviderKind.OpenAiCompatible,
                    profile.BaseUrl.Trim().TrimEnd('/')),
            _ => null
        };
    }

    public async Task SaveAsync(AgentProviderProfile configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string filePath = _pathProvider.GetConfigurationFilePath();
        string directoryPath = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("Configuration path does not contain a parent directory.");

        FilePermissionHelper.EnsurePrivateDirectory(directoryPath);

        await using FileStream stream = new(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.Asynchronous);

        await JsonSerializer.SerializeAsync(
            stream,
            configuration,
            OnboardingStorageJsonContext.Default.AgentProviderProfile,
            cancellationToken);

        await stream.FlushAsync(cancellationToken);
        FilePermissionHelper.EnsurePrivateFile(filePath);
    }
}
