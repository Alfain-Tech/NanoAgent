using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IToolExecutionPipeline
{
    Task<ToolExecutionBatchResult> ExecuteAsync(
        IReadOnlyList<ConversationToolCall> toolCalls,
        ReplSessionContext session,
        CancellationToken cancellationToken);
}
