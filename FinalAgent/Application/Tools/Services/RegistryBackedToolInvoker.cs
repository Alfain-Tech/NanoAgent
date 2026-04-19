using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Tools.Services;

internal sealed class RegistryBackedToolInvoker : IToolInvoker
{
    private readonly IToolRegistry _toolRegistry;

    public RegistryBackedToolInvoker(IToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public async Task<ToolInvocationResult> InvokeAsync(
        ConversationToolCall toolCall,
        ReplSessionContext session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_toolRegistry.TryResolve(toolCall.Name, out IAgentTool? tool) || tool is null)
        {
            return new ToolInvocationResult(
                toolCall.Id,
                toolCall.Name,
                ToolResult.NotFound(
                    $"Tool '{toolCall.Name}' is not registered in this agent."));
        }

        try
        {
            ToolResult result = await tool.ExecuteAsync(
                new ToolExecutionContext(
                    toolCall.Id,
                    toolCall.Name,
                    toolCall.ArgumentsJson,
                    session),
                cancellationToken);

            return new ToolInvocationResult(toolCall.Id, toolCall.Name, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new ToolInvocationResult(
                toolCall.Id,
                toolCall.Name,
                ToolResult.ExecutionError(
                    $"Tool execution failed unexpectedly: {exception.Message}"));
        }
    }
}
