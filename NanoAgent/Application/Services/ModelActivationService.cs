using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;

namespace NanoAgent.Application.Services;

internal sealed class ModelActivationService : IModelActivationService
{
    public ModelActivationResult Resolve(
        ReplSessionContext session,
        string requestedModel)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedModel);

        string normalizedRequestedModel = requestedModel.Trim();
        string[] exactMatches = session.AvailableModelIds
            .Where(modelId => string.Equals(modelId, normalizedRequestedModel, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (exactMatches.Length == 1)
        {
            return SwitchOrConfirm(session, exactMatches[0]);
        }

        string[] suffixMatches = session.AvailableModelIds
            .Where(modelId => HasMatchingTerminalSegment(modelId, normalizedRequestedModel))
            .ToArray();

        if (suffixMatches.Length == 1)
        {
            return SwitchOrConfirm(session, suffixMatches[0]);
        }

        if (suffixMatches.Length > 1)
        {
            return new ModelActivationResult(
                ModelActivationStatus.Ambiguous,
                null,
                suffixMatches);
        }

        return new ModelActivationResult(ModelActivationStatus.NotFound, null);
    }

    private static bool HasMatchingTerminalSegment(string modelId, string requestedModel)
    {
        int lastSlashIndex = modelId.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex == modelId.Length - 1)
        {
            return false;
        }

        return string.Equals(
            modelId[(lastSlashIndex + 1)..],
            requestedModel,
            StringComparison.OrdinalIgnoreCase);
    }

    private static ModelActivationResult SwitchOrConfirm(
        ReplSessionContext session,
        string resolvedModelId)
    {
        if (string.Equals(session.ActiveModelId, resolvedModelId, StringComparison.Ordinal))
        {
            return new ModelActivationResult(
                ModelActivationStatus.AlreadyActive,
                resolvedModelId);
        }

        session.SetActiveModel(resolvedModelId);

        return new ModelActivationResult(
            ModelActivationStatus.Switched,
            resolvedModelId);
    }
}
