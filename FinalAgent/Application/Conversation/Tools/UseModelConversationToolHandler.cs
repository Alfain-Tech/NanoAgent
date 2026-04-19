using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Conversation.Tools;

internal sealed class UseModelConversationToolHandler : IConversationToolHandler
{
    private readonly IModelActivationService _modelActivationService;

    public UseModelConversationToolHandler(IModelActivationService modelActivationService)
    {
        _modelActivationService = modelActivationService;
    }

    public string ToolName => ConversationToolNames.UseModel;

    public Task<string> ExecuteAsync(
        ConversationToolCall toolCall,
        ReplSessionContext session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        string? requestedModel = TryReadRequestedModel(toolCall.ArgumentsJson);
        if (string.IsNullOrWhiteSpace(requestedModel))
        {
            return Task.FromResult(
                "Tool 'use_model' requires a 'model' or 'modelId' string argument.");
        }

        ModelActivationResult result = _modelActivationService.Resolve(session, requestedModel);
        return Task.FromResult(result.Status switch
        {
            ModelActivationStatus.Switched =>
                $"Active model switched to '{result.ResolvedModelId}'.",
            ModelActivationStatus.AlreadyActive =>
                $"Already using '{result.ResolvedModelId}'.",
            ModelActivationStatus.Ambiguous =>
                "Model name is ambiguous. Matches: " + string.Join(", ", result.CandidateModelIds),
            _ =>
                $"Model '{requestedModel.Trim()}' is not available in the current session."
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
