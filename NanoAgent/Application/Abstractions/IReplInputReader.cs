namespace NanoAgent.Application.Abstractions;

public interface IReplInputReader
{
    Task<string?> ReadLineAsync(CancellationToken cancellationToken);
}
