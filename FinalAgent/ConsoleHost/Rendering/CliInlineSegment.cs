namespace FinalAgent.ConsoleHost.Rendering;

internal sealed class CliInlineSegment
{
    public CliInlineSegment(string text, CliInlineStyle style = CliInlineStyle.Plain)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        Style = style;
    }

    public CliInlineStyle Style { get; }

    public string Text { get; }
}
