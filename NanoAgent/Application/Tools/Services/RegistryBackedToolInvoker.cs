using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools.Serialization;

namespace NanoAgent.Application.Tools.Services;

internal sealed class RegistryBackedToolInvoker : IToolInvoker
{
    private readonly TimeSpan _defaultTimeout;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IToolRegistry _toolRegistry;

    public RegistryBackedToolInvoker(
        IToolRegistry toolRegistry,
        IPermissionEvaluator permissionEvaluator,
        TimeSpan? defaultTimeout = null)
    {
        _toolRegistry = toolRegistry;
        _permissionEvaluator = permissionEvaluator;
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
    }

    public async Task<ToolInvocationResult> InvokeAsync(
        ConversationToolCall toolCall,
        ReplSessionContext session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_toolRegistry.TryResolve(toolCall.Name, out ToolRegistration? registration) || registration is null)
        {
            return new ToolInvocationResult(
                toolCall.Id,
                toolCall.Name,
                ToolResultFactory.NotFound(
                    "tool_not_found",
                    $"Tool '{toolCall.Name}' is not registered in this agent.",
                    new ToolRenderPayload(
                        $"Unknown tool: {toolCall.Name}",
                        $"The LLM requested '{toolCall.Name}', but this agent does not have that tool registered.")));
        }

        JsonElement arguments;

        try
        {
            arguments = ParseArguments(toolCall);
        }
        catch (JsonException exception)
        {
            return new ToolInvocationResult(
                toolCall.Id,
                toolCall.Name,
                ToolResultFactory.InvalidArguments(
                    "invalid_json_arguments",
                    $"Tool '{toolCall.Name}' received invalid JSON arguments: {exception.Message}",
                    new ToolRenderPayload(
                        $"Invalid tool arguments: {toolCall.Name}",
                        $"The LLM produced malformed JSON arguments for '{toolCall.Name}'.")));
        }
        catch (InvalidOperationException exception)
        {
            return new ToolInvocationResult(
                toolCall.Id,
                toolCall.Name,
                ToolResultFactory.InvalidArguments(
                    "invalid_tool_arguments",
                    exception.Message,
                    new ToolRenderPayload(
                        $"Invalid tool arguments: {toolCall.Name}",
                        exception.Message)));
        }

        ToolExecutionContext executionContext = new(
            toolCall.Id,
            toolCall.Name,
            arguments,
            session);

        PermissionEvaluationResult permissionResult = _permissionEvaluator.Evaluate(
            registration.PermissionPolicy,
            new PermissionEvaluationContext(executionContext));

        if (!permissionResult.IsAllowed)
        {
            string reasonCode = permissionResult.ReasonCode!;
            string reason = permissionResult.Reason!;
            string title = permissionResult.Decision == PermissionEvaluationDecision.RequiresApproval
                ? $"Approval required: {toolCall.Name}"
                : $"Permission denied: {toolCall.Name}";

            return new ToolInvocationResult(
                toolCall.Id,
                toolCall.Name,
                ToolResultFactory.PermissionDenied(
                    reasonCode,
                    reason,
                    new ToolRenderPayload(
                        title,
                        reason)));
        }

        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_defaultTimeout);

        try
        {
            ToolResult result = await registration.Tool.ExecuteAsync(
                executionContext,
                timeoutSource.Token);

            return new ToolInvocationResult(toolCall.Id, toolCall.Name, result);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            return new ToolInvocationResult(
                toolCall.Id,
                toolCall.Name,
                ToolResultFactory.ExecutionError(
                    "tool_timeout",
                    $"Tool '{toolCall.Name}' timed out after {_defaultTimeout.TotalSeconds:0} seconds.",
                    new ToolRenderPayload(
                        $"Tool timed out: {toolCall.Name}",
                        $"'{toolCall.Name}' did not finish within {_defaultTimeout.TotalSeconds:0} seconds.")));
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
                ToolResultFactory.ExecutionError(
                    "tool_execution_failed",
                    $"Tool execution failed unexpectedly: {exception.Message}",
                    new ToolRenderPayload(
                        $"Tool failed: {toolCall.Name}",
                        exception.Message)));
        }
    }

    private static JsonElement ParseArguments(ConversationToolCall toolCall)
    {
        if (string.IsNullOrWhiteSpace(toolCall.ArgumentsJson))
        {
            throw new InvalidOperationException(
                $"Tool '{toolCall.Name}' must receive JSON-object arguments.");
        }

        using JsonDocument argumentsDocument = JsonDocument.Parse(toolCall.ArgumentsJson);
        if (argumentsDocument.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Tool '{toolCall.Name}' must receive JSON-object arguments.");
        }

        return argumentsDocument.RootElement.Clone();
    }
}
