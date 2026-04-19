namespace NanoAgent.Application.Models;

public sealed class ToolFeedbackRenderPayload
{
    public ToolFeedbackRenderPayload(string title, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Title = title.Trim();
        Text = text.Trim();
    }

    public string Text { get; }

    public string Title { get; }
}
