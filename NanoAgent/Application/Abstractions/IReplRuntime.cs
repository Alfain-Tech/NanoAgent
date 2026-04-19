using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IReplRuntime
{
    Task RunAsync(ReplSessionContext session, CancellationToken cancellationToken);
}
