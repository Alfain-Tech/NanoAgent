namespace FinalAgent.Application.Models;

public sealed class ToolExecutionContext
{
    public ToolExecutionContext(
        string toolCallId,
        string toolName,
        string argumentsJson,
        ReplSessionContext session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(argumentsJson);
        ArgumentNullException.ThrowIfNull(session);

        ToolCallId = toolCallId.Trim();
        ToolName = toolName.Trim();
        ArgumentsJson = argumentsJson;
        Session = session;
    }

    public string ArgumentsJson { get; }

    public ReplSessionContext Session { get; }

    public string ToolCallId { get; }

    public string ToolName { get; }
}
