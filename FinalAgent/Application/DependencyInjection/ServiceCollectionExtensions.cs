using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Conversation.Services;
using FinalAgent.Application.Conversation.Tools;
using FinalAgent.Application.Repl.Commands;
using FinalAgent.Application.Repl.Parsing;
using FinalAgent.Application.Repl.Services;
using FinalAgent.Application.Services;
using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinalAgent.Application.DependencyInjection;

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
        services.AddSingleton<IToolExecutionPipeline, ToolExecutionPipeline>();
        services.AddSingleton<IConversationToolHandler, ShowConfigConversationToolHandler>();
        services.AddSingleton<IConversationToolHandler, ListModelsConversationToolHandler>();
        services.AddSingleton<IConversationToolHandler, UseModelConversationToolHandler>();
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
        services.AddSingleton<IModelSelectionPolicy, RankedModelSelectionPolicy>();

        return services;
    }
}
