using NanoAgent.Application.Tools.Models;

namespace NanoAgent.Application.Abstractions;

public interface IShellCommandService
{
    Task<ShellCommandExecutionResult> ExecuteAsync(
        ShellCommandExecutionRequest request,
        CancellationToken cancellationToken);
}
