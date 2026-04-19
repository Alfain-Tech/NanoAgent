namespace NanoAgent.Application.Abstractions;

public interface IReplOutputWriter
{
    Task WriteShellHeaderAsync(
        string applicationName,
        string modelName,
        CancellationToken cancellationToken);

    Task WriteInfoAsync(string message, CancellationToken cancellationToken);

    Task WriteErrorAsync(string message, CancellationToken cancellationToken);

    Task WriteWarningAsync(string message, CancellationToken cancellationToken);

    Task WriteResponseAsync(string message, CancellationToken cancellationToken);
}
