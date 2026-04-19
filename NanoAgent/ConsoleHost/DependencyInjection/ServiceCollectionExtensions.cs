using NanoAgent.Application.Abstractions;
using NanoAgent.ConsoleHost.Hosting;
using NanoAgent.ConsoleHost.Prompts;
using NanoAgent.ConsoleHost.Repl;
using NanoAgent.ConsoleHost.Rendering;
using NanoAgent.ConsoleHost.Terminal;
using Microsoft.Extensions.DependencyInjection;

namespace NanoAgent.ConsoleHost.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleHost(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IConsoleTerminal, ConsoleTerminal>();
        services.AddSingleton<IConsolePromptRenderer, ConsolePromptRenderer>();
        services.AddSingleton<IConsolePromptInputReader, ConsolePromptInputReader>();
        services.AddSingleton<ICliOutputTarget, ConsoleCliOutputTarget>();
        services.AddSingleton<ICliMessageFormatter, MarkdownLikeCliMessageFormatter>();
        services.AddSingleton<ICliTextRenderer, CliTextRenderer>();
        services.AddSingleton<ISelectionPrompt, ConsoleSelectionPrompt>();
        services.AddSingleton<ITextPrompt, ConsoleTextPrompt>();
        services.AddSingleton<ISecretPrompt, ConsoleSecretPrompt>();
        services.AddSingleton<IConfirmationPrompt, ConsoleConfirmationPrompt>();
        services.AddSingleton<IStatusMessageWriter, ConsoleStatusMessageWriter>();
        services.AddSingleton<IReplInputReader, ConsoleReplInputReader>();
        services.AddSingleton<IReplOutputWriter, ConsoleReplOutputWriter>();
        services.AddSingleton<ProcessExitCodeTracker>();
        services.AddHostedService<ConsoleApplicationHostedService>();

        return services;
    }
}
