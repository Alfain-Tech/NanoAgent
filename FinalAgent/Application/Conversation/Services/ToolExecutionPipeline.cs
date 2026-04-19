using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Conversation.Services;

internal sealed class ToolExecutionPipeline : IToolExecutionPipeline
{
    private readonly IReadOnlyDictionary<string, IConversationToolHandler> _handlers;

    public ToolExecutionPipeline(IEnumerable<IConversationToolHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _handlers = handlers.ToDictionary(
            handler => handler.ToolName,
            StringComparer.Ordinal);
    }

    public async Task<ConversationTurnResult> ExecuteAsync(
        IReadOnlyList<ConversationToolCall> toolCalls,
        ReplSessionContext session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        if (toolCalls.Count == 0)
        {
            return ConversationTurnResult.ToolExecution(
                "The provider requested tool execution, but no tool calls were included.");
        }

        List<string> outputLines = [];

        foreach (ConversationToolCall toolCall in toolCalls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_handlers.TryGetValue(toolCall.Name, out IConversationToolHandler? handler))
            {
                outputLines.Add($"Tool '{toolCall.Name}' is not supported by this agent.");
                continue;
            }

            try
            {
                string toolOutput = await handler.ExecuteAsync(
                    toolCall,
                    session,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(toolOutput))
                {
                    outputLines.Add(toolOutput.Trim());
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                outputLines.Add(
                    $"Tool '{toolCall.Name}' failed unexpectedly: {exception.Message}");
            }
        }

        string message = outputLines.Count == 0
            ? "Tool execution completed with no visible output."
            : string.Join(Environment.NewLine + Environment.NewLine, outputLines);

        return ConversationTurnResult.ToolExecution(message);
    }
}
