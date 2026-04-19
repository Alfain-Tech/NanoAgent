using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Conversation.Tools;

internal sealed class ListModelsConversationToolHandler : IConversationToolHandler
{
    public string ToolName => ConversationToolNames.ListModels;

    public Task<string> ExecuteAsync(
        ConversationToolCall toolCall,
        ReplSessionContext session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        string[] lines =
        [
            $"Available models ({session.AvailableModelIds.Count}):",
            .. session.AvailableModelIds.Select(modelId =>
                modelId == session.ActiveModelId
                    ? $"- {modelId} (active)"
                    : $"- {modelId}")
        ];

        return Task.FromResult(string.Join(Environment.NewLine, lines));
    }
}
