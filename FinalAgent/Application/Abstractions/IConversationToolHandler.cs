using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IConversationToolHandler
{
    string ToolName { get; }

    Task<string> ExecuteAsync(
        ConversationToolCall toolCall,
        ReplSessionContext session,
        CancellationToken cancellationToken);
}
