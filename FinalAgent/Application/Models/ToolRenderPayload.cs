namespace FinalAgent.Application.Models;

public sealed class ToolRenderPayload
{
    public ToolRenderPayload(string title, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Title = title.Trim();
        Text = text.Trim();
    }

    public string Text { get; }

    public string Title { get; }
}
