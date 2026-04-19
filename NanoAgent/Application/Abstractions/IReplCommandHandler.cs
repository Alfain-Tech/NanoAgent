using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IReplCommandHandler
{
    string CommandName { get; }

    string Description { get; }

    string Usage { get; }

    Task<ReplCommandResult> ExecuteAsync(
        ReplCommandContext context,
        CancellationToken cancellationToken);
}
