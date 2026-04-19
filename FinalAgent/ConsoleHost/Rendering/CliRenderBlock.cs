namespace FinalAgent.ConsoleHost.Rendering;

internal sealed class CliRenderBlock
{
    public CliRenderBlock(
        CliRenderBlockKind kind,
        IReadOnlyList<CliRenderLine> lines,
        string? language = null,
        int headingLevel = 0)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            throw new ArgumentException(
                "Render blocks must contain at least one line.",
                nameof(lines));
        }

        Kind = kind;
        Lines = lines;
        Language = string.IsNullOrWhiteSpace(language)
            ? null
            : language.Trim();
        HeadingLevel = headingLevel;
    }

    public int HeadingLevel { get; }

    public CliRenderBlockKind Kind { get; }

    public string? Language { get; }

    public IReadOnlyList<CliRenderLine> Lines { get; }
}
