using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IToolExecutionPipeline
{
    Task<ConversationTurnResult> ExecuteAsync(
        IReadOnlyList<ConversationToolCall> toolCalls,
        ReplSessionContext session,
        CancellationToken cancellationToken);
}
