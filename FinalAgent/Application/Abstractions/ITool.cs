using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface ITool
{
    string Description { get; }

    string Name { get; }

    string PermissionRequirements { get; }

    string Schema { get; }

    Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken);
}
