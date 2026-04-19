namespace NanoAgent.Application.Models;

public sealed record ConversationResponse(
    string? AssistantMessage,
    IReadOnlyList<ConversationToolCall> ToolCalls,
    string? ResponseId,
    int? CompletionTokens = null)
{
    public bool HasToolCalls => ToolCalls.Count > 0;
}
