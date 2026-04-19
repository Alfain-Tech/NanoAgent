using FinalAgent.Application.Abstractions;
using FinalAgent.ConsoleHost.Rendering;

namespace FinalAgent.ConsoleHost.Repl;

internal sealed class ConsoleReplOutputWriter : IReplOutputWriter
{
    private const int HeaderDividerWidth = 53;
    private const string RepositoryUrl = "github.com/rizwan3d/NanoAgent";
    private const string SponsorName = "ALFAIN Technologies (PVT) Limited";
    private const string SponsorUrl = "https://alfain.co/";

    private readonly ICliMessageFormatter _formatter;
    private readonly ICliTextRenderer _renderer;
    private readonly ICliOutputTarget _outputTarget;

    public ConsoleReplOutputWriter(
        ICliMessageFormatter formatter,
        ICliTextRenderer renderer,
        ICliOutputTarget outputTarget)
    {
        _formatter = formatter;
        _renderer = renderer;
        _outputTarget = outputTarget;
    }

    public Task WriteErrorAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _renderer.RenderAsync(
            _formatter.Format(CliRenderMessageKind.Error, message),
            cancellationToken);
    }

    public Task WriteShellHeaderAsync(
        string applicationName,
        string modelName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        _outputTarget.WriteLine();
        _outputTarget.WriteLine([
            new CliOutputSegment("  ", CliOutputStyle.Muted),
            new CliOutputSegment(applicationName.Trim(), CliOutputStyle.Warning)
        ]);
        _outputTarget.WriteLine([
            new CliOutputSegment("  Model: ", CliOutputStyle.Muted),
            new CliOutputSegment(modelName.Trim(), CliOutputStyle.InlineCode)
        ]);
        _outputTarget.WriteLine([
            new CliOutputSegment("  GitHub: ", CliOutputStyle.Muted),
            new CliOutputSegment(RepositoryUrl, CliOutputStyle.Info)
        ]);
        _outputTarget.WriteLine([
            new CliOutputSegment("  Sponsor: ", CliOutputStyle.Muted),
            new CliOutputSegment(SponsorName, CliOutputStyle.Warning),
            new CliOutputSegment(" ", CliOutputStyle.Muted),
            new CliOutputSegment($"({SponsorUrl})", CliOutputStyle.Emphasis)
        ]);
        _outputTarget.WriteLine([
            new CliOutputSegment("  ", CliOutputStyle.Muted),
            new CliOutputSegment(new string('\u2500', HeaderDividerWidth), CliOutputStyle.CodeFence)
        ]);
        _outputTarget.WriteLine([
            new CliOutputSegment(
                "  Chat in the terminal. Press Ctrl+C or use /exit to quit.",
                CliOutputStyle.Muted)
        ]);
        _outputTarget.WriteLine();

        return Task.CompletedTask;
    }

    public Task WriteInfoAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _renderer.RenderAsync(
            _formatter.Format(CliRenderMessageKind.Info, message),
            cancellationToken);
    }

    public Task WriteWarningAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _renderer.RenderAsync(
            _formatter.Format(CliRenderMessageKind.Warning, message),
            cancellationToken);
    }

    public Task WriteResponseAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _renderer.RenderAsync(
            _formatter.Format(CliRenderMessageKind.Assistant, message),
            cancellationToken);
    }
}
