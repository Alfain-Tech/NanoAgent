namespace FinalAgent.Application.Models;

public sealed class ConversationTurnResult
{
    public ConversationTurnResult(string responseText)
        : this(ConversationTurnResultKind.AssistantMessage, responseText)
    {
    }

    public ConversationTurnResult(
        ConversationTurnResultKind kind,
        string responseText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(responseText);

        Kind = kind;
        ResponseText = responseText.Trim();
    }

    public ConversationTurnResultKind Kind { get; }

    public string ResponseText { get; }

    public static ConversationTurnResult AssistantMessage(string responseText)
    {
        return new ConversationTurnResult(
            ConversationTurnResultKind.AssistantMessage,
            responseText);
    }

    public static ConversationTurnResult ToolExecution(string responseText)
    {
        return new ConversationTurnResult(
            ConversationTurnResultKind.ToolExecution,
            responseText);
    }
}
