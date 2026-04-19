using FinalAgent.Application.Tools.Models;

namespace FinalAgent.Application.Abstractions;

public interface IShellCommandService
{
    Task<ShellCommandExecutionResult> ExecuteAsync(
        ShellCommandExecutionRequest request,
        CancellationToken cancellationToken);
}
