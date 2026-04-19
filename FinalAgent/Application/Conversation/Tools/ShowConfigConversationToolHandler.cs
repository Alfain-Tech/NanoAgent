using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Conversation.Tools;

internal sealed class ShowConfigConversationToolHandler : IConversationToolHandler
{
    private const string OpenAiBaseUrl = "https://api.openai.com/v1/";

    private readonly IUserDataPathProvider _userDataPathProvider;

    public ShowConfigConversationToolHandler(IUserDataPathProvider userDataPathProvider)
    {
        _userDataPathProvider = userDataPathProvider;
    }

    public string ToolName => ConversationToolNames.ShowConfig;

    public Task<string> ExecuteAsync(
        ConversationToolCall toolCall,
        ReplSessionContext session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        string baseUrl = session.ProviderProfile.ProviderKind == ProviderKind.OpenAi
            ? OpenAiBaseUrl
            : session.ProviderProfile.BaseUrl ?? "(not configured)";

        string message =
            "Current configuration:" + Environment.NewLine +
            $"Provider: {session.ProviderName}" + Environment.NewLine +
            $"Base URL: {baseUrl}" + Environment.NewLine +
            $"Configuration file: {_userDataPathProvider.GetConfigurationFilePath()}" + Environment.NewLine +
            $"Active model: {session.ActiveModelId}";

        return Task.FromResult(message);
    }
}
