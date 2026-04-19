using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Tools;

internal sealed class ListModelsTool : IAgentTool
{
    public string Name => AgentToolNames.ListModels;

    public Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        string[] lines =
        [
            $"Available models ({context.Session.AvailableModelIds.Count}):",
            .. context.Session.AvailableModelIds.Select(modelId =>
                modelId == context.Session.ActiveModelId
                    ? $"- {modelId} (active)"
                    : $"- {modelId}")
        ];

        return Task.FromResult(ToolResult.Success(
            string.Join(Environment.NewLine, lines)));
    }
}
