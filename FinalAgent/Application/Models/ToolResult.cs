namespace FinalAgent.Application.Models;

public sealed class ToolResult
{
    public ToolResult(
        ToolResultStatus status,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Status = status;
        Message = message.Trim();
    }

    public bool IsSuccess => Status == ToolResultStatus.Success;

    public string Message { get; }

    public ToolResultStatus Status { get; }

    public static ToolResult ExecutionError(string message)
    {
        return new ToolResult(ToolResultStatus.ExecutionError, message);
    }

    public static ToolResult InvalidArguments(string message)
    {
        return new ToolResult(ToolResultStatus.InvalidArguments, message);
    }

    public static ToolResult NotFound(string message)
    {
        return new ToolResult(ToolResultStatus.NotFound, message);
    }

    public static ToolResult Success(string message)
    {
        return new ToolResult(ToolResultStatus.Success, message);
    }
}
