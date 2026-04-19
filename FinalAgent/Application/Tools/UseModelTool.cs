using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Tools;

internal sealed class UseModelTool : IAgentTool
{
    private readonly IModelActivationService _modelActivationService;

    public UseModelTool(IModelActivationService modelActivationService)
    {
        _modelActivationService = modelActivationService;
    }

    public string Name => AgentToolNames.UseModel;

    public Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        string? requestedModel;

        try
        {
            requestedModel = TryReadRequestedModel(context.ArgumentsJson);
        }
        catch (JsonException)
        {
            return Task.FromResult(ToolResult.InvalidArguments(
                "Tool 'use_model' received invalid JSON arguments."));
        }

        if (string.IsNullOrWhiteSpace(requestedModel))
        {
            return Task.FromResult(ToolResult.InvalidArguments(
                "Tool 'use_model' requires a 'model' or 'modelId' string argument."));
        }

        ModelActivationResult result = _modelActivationService.Resolve(
            context.Session,
            requestedModel);

        return Task.FromResult(result.Status switch
        {
            ModelActivationStatus.Switched =>
                ToolResult.Success(
                    $"Active model switched to '{result.ResolvedModelId}'."),
            ModelActivationStatus.AlreadyActive =>
                ToolResult.Success(
                    $"Already using '{result.ResolvedModelId}'."),
            ModelActivationStatus.Ambiguous =>
                ToolResult.InvalidArguments(
                    "Model name is ambiguous. Matches: " + string.Join(", ", result.CandidateModelIds)),
            _ =>
                ToolResult.InvalidArguments(
                    $"Model '{requestedModel.Trim()}' is not available in the current session.")
        });
    }

    private static string? TryReadRequestedModel(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (TryGetString(root, "model", out string? model))
        {
            return model;
        }

        return TryGetString(root, "modelId", out string? modelId)
            ? modelId
            : null;
    }

    private static bool TryGetString(
        JsonElement element,
        string propertyName,
        out string? value)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return true;
        }

        value = null;
        return false;
    }
}
