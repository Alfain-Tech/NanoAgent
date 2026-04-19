using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Conversation.Services;
using NanoAgent.Application.Permissions;
using NanoAgent.Application.Repl.Commands;
using NanoAgent.Application.Repl.Parsing;
using NanoAgent.Application.Repl.Services;
using NanoAgent.Application.Services;
using NanoAgent.Application.Tools;
using NanoAgent.Application.Tools.Services;
using NanoAgent.Domain.Abstractions;
using NanoAgent.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace NanoAgent.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IApplicationRunner, AgentApplicationRunner>();
        services.AddSingleton<IReplRuntime, ReplRuntime>();
        services.AddSingleton<IReplCommandParser, ReplCommandParser>();
        services.AddSingleton<IReplCommandDispatcher, ReplCommandDispatcher>();
        services.AddSingleton<IConversationPipeline, AgentConversationPipeline>();
        services.AddSingleton<IPermissionParser, ToolPermissionParser>();
        services.AddSingleton<IPermissionEvaluator, ToolPermissionEvaluator>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<IToolInvoker, RegistryBackedToolInvoker>();
        services.AddSingleton<IToolExecutionPipeline, ToolExecutionPipeline>();
        services.AddSingleton<ITool, FileReadTool>();
        services.AddSingleton<ITool, FileWriteTool>();
        services.AddSingleton<ITool, DirectoryListTool>();
        services.AddSingleton<ITool, TextSearchTool>();
        services.AddSingleton<ITool, ShellCommandTool>();
        services.AddSingleton<IReplCommandHandler, ConfigCommandHandler>();
        services.AddSingleton<IReplCommandHandler, HelpCommandHandler>();
        services.AddSingleton<IReplCommandHandler, ModelsCommandHandler>();
        services.AddSingleton<IReplCommandHandler, UseModelCommandHandler>();
        services.AddSingleton<IReplCommandHandler, ExitCommandHandler>();
        services.AddSingleton<IModelDiscoveryService, ModelDiscoveryService>();
        services.AddSingleton<IFirstRunOnboardingService, FirstRunOnboardingService>();
        services.AddSingleton<IOnboardingInputValidator, OnboardingInputValidator>();
        services.AddSingleton<IModelActivationService, ModelActivationService>();
        services.AddSingleton<IAgentProviderProfileFactory, AgentProviderProfileFactory>();
        services.AddSingleton<IModelSelectionPolicy, ConfiguredOrFirstModelSelectionPolicy>();

        return services;
    }
}
