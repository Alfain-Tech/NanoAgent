using FinalAgent.Application.Abstractions;
using FinalAgent.ConsoleHost.Rendering;

namespace FinalAgent.ConsoleHost.Repl;

internal sealed class ConsoleReplOutputWriter : IReplOutputWriter
{
    private readonly ICliMessageFormatter _formatter;
    private readonly ICliTextRenderer _renderer;

    public ConsoleReplOutputWriter(
        ICliMessageFormatter formatter,
        ICliTextRenderer renderer)
    {
        _formatter = formatter;
        _renderer = renderer;
    }

    public Task WriteErrorAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _renderer.RenderAsync(
            _formatter.Format(CliRenderMessageKind.Error, message),
            cancellationToken);
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
