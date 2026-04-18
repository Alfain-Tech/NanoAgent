using FinalAgent.Application.Abstractions;
using FinalAgent.ConsoleHost.Hosting;
using FinalAgent.ConsoleHost.Output;
using Microsoft.Extensions.DependencyInjection;

namespace FinalAgent.ConsoleHost.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleHost(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IGreetingSink, LoggingGreetingSink>();
        services.AddSingleton<ProcessExitCodeTracker>();
        services.AddHostedService<ConsoleApplicationHostedService>();

        return services;
    }
}
