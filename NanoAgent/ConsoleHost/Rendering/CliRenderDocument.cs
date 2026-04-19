namespace NanoAgent.ConsoleHost.Rendering;

internal sealed class CliRenderDocument
{
    public CliRenderDocument(
        CliRenderMessageKind kind,
        IReadOnlyList<CliRenderBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        Kind = kind;
        Blocks = blocks;
    }

    public IReadOnlyList<CliRenderBlock> Blocks { get; }

    public CliRenderMessageKind Kind { get; }
}
