using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IToolInvoker
{
    Task<ToolInvocationResult> InvokeAsync(
        ConversationToolCall toolCall,
        ReplSessionContext session,
        CancellationToken cancellationToken);
}
