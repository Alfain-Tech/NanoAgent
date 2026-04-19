using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Logging;
using FinalAgent.Application.Models;
using Microsoft.Extensions.Logging;

namespace FinalAgent.Application.Conversation.Services;

internal sealed class AgentConversationPipeline : IConversationPipeline
{
    private readonly IApiKeySecretStore _secretStore;
    private readonly IConversationProviderClient _providerClient;
    private readonly IConversationResponseMapper _responseMapper;
    private readonly IToolExecutionPipeline _toolExecutionPipeline;
    private readonly IConversationConfigurationAccessor _configurationAccessor;
    private readonly ILogger<AgentConversationPipeline> _logger;

    public AgentConversationPipeline(
        IApiKeySecretStore secretStore,
        IConversationProviderClient providerClient,
        IConversationResponseMapper responseMapper,
        IToolExecutionPipeline toolExecutionPipeline,
        IConversationConfigurationAccessor configurationAccessor,
        ILogger<AgentConversationPipeline> logger)
    {
        _secretStore = secretStore;
        _providerClient = providerClient;
        _responseMapper = responseMapper;
        _toolExecutionPipeline = toolExecutionPipeline;
        _configurationAccessor = configurationAccessor;
        _logger = logger;
    }

    public async Task<ConversationTurnResult> ProcessAsync(
        string input,
        ReplSessionContext session,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        string apiKey = await _secretStore.LoadAsync(cancellationToken)
            ?? throw new ConversationPipelineException(
                "Conversation cannot start because the API key is missing.");

        ConversationSettings settings = _configurationAccessor.GetSettings();
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(settings.RequestTimeout);

        ApplicationLogMessages.ConversationRequestStarted(
            _logger,
            session.ProviderName,
            session.ActiveModelId);

        ConversationProviderPayload providerPayload;

        try
        {
            providerPayload = await _providerClient.SendAsync(
                new ConversationProviderRequest(
                    session.ProviderProfile,
                    apiKey,
                    session.ActiveModelId,
                    input.Trim(),
                    settings.SystemPrompt),
                timeoutSource.Token);
        }
        catch (ConversationProviderException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new ConversationProviderException(
                $"The conversation request timed out after {settings.RequestTimeout.TotalSeconds:0} seconds.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ConversationProviderException(
                "The configured provider failed while processing the conversation request.",
                exception);
        }

        ConversationResponse response;

        try
        {
            response = _responseMapper.Map(providerPayload);
        }
        catch (ConversationResponseException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ConversationResponseException(
                "The provider response could not be normalized into the internal conversation model.",
                exception);
        }

        if (response.HasToolCalls)
        {
            ApplicationLogMessages.ConversationToolHandoffStarted(
                _logger,
                response.ToolCalls.Count);

            ConversationTurnResult toolResult = await _toolExecutionPipeline.ExecuteAsync(
                response.ToolCalls,
                session,
                cancellationToken);

            ApplicationLogMessages.ConversationToolHandoffCompleted(_logger);
            return toolResult;
        }

        if (string.IsNullOrWhiteSpace(response.AssistantMessage))
        {
            throw new ConversationResponseException(
                "The provider response did not contain an assistant message or any tool calls.");
        }

        ApplicationLogMessages.ConversationAssistantMessageReceived(_logger);

        return ConversationTurnResult.AssistantMessage(response.AssistantMessage);
    }
}
