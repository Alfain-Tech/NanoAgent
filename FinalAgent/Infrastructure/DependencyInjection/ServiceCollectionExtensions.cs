using FinalAgent.Infrastructure.Configuration;
using FinalAgent.Infrastructure.Conversation;
using FinalAgent.Infrastructure.Logging;
using FinalAgent.Infrastructure.Secrets;
using FinalAgent.Application.Abstractions;
using FinalAgent.Infrastructure.Models;
using FinalAgent.Infrastructure.Storage;
using FinalAgent.Infrastructure.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinalAgent.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IUserDataPathProvider, UserDataPathProvider>();
        services.AddSingleton<IWorkspaceRootProvider, CurrentDirectoryWorkspaceRootProvider>();
        services.AddSingleton<IWorkspaceFileService, WorkspaceFileService>();
        services.AddSingleton<IShellCommandService, ShellCommandService>();
        services.AddSingleton<IAgentConfigurationStore, JsonAgentConfigurationStore>();
        services.AddSingleton<IApiKeySecretStore, ApiKeySecretStore>();
        services.AddSingleton<IModelCache, InMemoryModelCache>();
        services.AddSingleton<IConversationConfigurationAccessor, ConversationConfigurationAccessor>();
        services.AddSingleton<IConversationResponseMapper, OpenAiConversationResponseMapper>();
        services.AddSingleton<IModelSelectionConfigurationAccessor, ModelSelectionConfigurationAccessor>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IPlatformCredentialStore>(CreatePlatformCredentialStore());
        services.AddSingleton<ILoggerProvider, DailyFileLoggerProvider>();
        services.AddSingleton<IValidateOptions<ApplicationOptions>, ApplicationOptionsValidator>();
        services.AddHttpClient<IConversationProviderClient, OpenAiCompatibleConversationProviderClient>();
        services.AddHttpClient<IModelProviderClient, OpenAiCompatibleModelProviderClient>();

        services
            .AddOptions<ApplicationOptions>()
            .BindConfiguration(ApplicationOptions.SectionName, binderOptions =>
            {
                binderOptions.ErrorOnUnknownConfiguration = true;
            })
            .ValidateOnStart();

        return services;
    }

    private static Func<IServiceProvider, IPlatformCredentialStore> CreatePlatformCredentialStore()
    {
        if (OperatingSystem.IsWindows())
        {
            return _ => new WindowsCredentialStore();
        }

        if (OperatingSystem.IsMacOS())
        {
            return _ => new MacOsKeychainCredentialStore();
        }

        if (OperatingSystem.IsLinux())
        {
            return serviceProvider => new LinuxSecretToolCredentialStore(
                serviceProvider.GetRequiredService<IProcessRunner>());
        }

        return _ => new UnsupportedPlatformCredentialStore();
    }
}
