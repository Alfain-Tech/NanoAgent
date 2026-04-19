namespace FinalAgent.Application.Models;

public sealed record ConversationResponse(
    string? AssistantMessage,
    IReadOnlyList<ConversationToolCall> ToolCalls,
    string? ResponseId)
{
    public bool HasToolCalls => ToolCalls.Count > 0;
}
