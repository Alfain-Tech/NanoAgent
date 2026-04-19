namespace NanoAgent.ConsoleHost.Rendering;

internal sealed class CliRenderLine
{
    public CliRenderLine(
        IReadOnlyList<CliInlineSegment> segments,
        CliRenderLineKind kind = CliRenderLineKind.Normal)
    {
        ArgumentNullException.ThrowIfNull(segments);

        if (segments.Count == 0)
        {
            throw new ArgumentException(
                "Render lines must contain at least one segment.",
                nameof(segments));
        }

        Segments = segments;
        Kind = kind;
    }

    public CliRenderLineKind Kind { get; }

    public IReadOnlyList<CliInlineSegment> Segments { get; }
}
