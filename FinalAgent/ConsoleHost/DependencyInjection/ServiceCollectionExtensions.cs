using FinalAgent.ConsoleHost.Hosting;
using FinalAgent.ConsoleHost.Prompts;
using FinalAgent.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FinalAgent.ConsoleHost.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleHost(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IUserPrompt, ConsoleUserPrompt>();
        services.AddSingleton<ProcessExitCodeTracker>();
        services.AddHostedService<ConsoleApplicationHostedService>();

        return services;
    }
}
