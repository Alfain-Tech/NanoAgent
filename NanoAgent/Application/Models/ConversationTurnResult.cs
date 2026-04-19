namespace NanoAgent.Application.Models;

public sealed class ConversationTurnResult
{
    public ConversationTurnResult(
        ConversationTurnResultKind kind,
        string responseText,
        ToolExecutionBatchResult? toolExecutionResult)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(responseText);

        Kind = kind;
        ResponseText = responseText.Trim();
        ToolExecutionResult = toolExecutionResult;
    }

    public ConversationTurnResult(string responseText)
        : this(ConversationTurnResultKind.AssistantMessage, responseText, null)
    {
    }

    public ConversationTurnResult(
        ConversationTurnResultKind kind,
        string responseText)
        : this(kind, responseText, null)
    {
    }

    public ConversationTurnResultKind Kind { get; }

    public string ResponseText { get; }

    public ToolExecutionBatchResult? ToolExecutionResult { get; }

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

    public static ConversationTurnResult ToolExecution(ToolExecutionBatchResult toolExecutionResult)
    {
        ArgumentNullException.ThrowIfNull(toolExecutionResult);

        return new ConversationTurnResult(
            ConversationTurnResultKind.ToolExecution,
            toolExecutionResult.ToDisplayText(),
            toolExecutionResult);
    }
}
