using System.Text.Json;

namespace NanoAgent.Application.Models;

public sealed class ToolFeedbackPayload
{
    public ToolFeedbackPayload(
        string toolName,
        ToolResultStatus status,
        bool isSuccess,
        string message,
        JsonElement data,
        ToolFeedbackRenderPayload? render = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        ToolName = toolName.Trim();
        Status = status;
        IsSuccess = isSuccess;
        Message = message.Trim();
        Data = data.Clone();
        Render = render;
    }

    public JsonElement Data { get; }

    public bool IsSuccess { get; }

    public string Message { get; }

    public ToolFeedbackRenderPayload? Render { get; }

    public ToolResultStatus Status { get; }

    public string ToolName { get; }
}
