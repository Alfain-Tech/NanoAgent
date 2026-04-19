namespace NanoAgent.Application.Abstractions;

public interface IApplicationRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}
