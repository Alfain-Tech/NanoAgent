using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IAgentTool
{
    string Name { get; }

    Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken);
}
