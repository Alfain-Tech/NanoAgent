namespace NanoAgent.ConsoleHost.Rendering;

internal interface ICliOutputTarget
{
    bool SupportsColor { get; }

    void WriteLine();

    void WriteLine(IReadOnlyList<CliOutputSegment> segments);
}
