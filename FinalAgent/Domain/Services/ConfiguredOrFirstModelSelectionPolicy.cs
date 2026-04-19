using FinalAgent.Application.Exceptions;
using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;

namespace FinalAgent.Domain.Services;

internal sealed class ConfiguredOrFirstModelSelectionPolicy : IModelSelectionPolicy
{
    public ModelSelectionDecision Select(ModelSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.AvailableModels.Count == 0)
        {
            throw new ModelSelectionException(
                "Model selection cannot run because the provider returned no available models.");
        }

        string? configuredDefaultModel = Normalize(context.ConfiguredDefaultModel);
        string? matchedConfiguredDefaultModel = ResolvePreferredModelId(
            context.AvailableModels,
            configuredDefaultModel);

        if (matchedConfiguredDefaultModel is not null)
        {
            return new ModelSelectionDecision(
                matchedConfiguredDefaultModel,
                ModelSelectionSource.ConfiguredDefault,
                ConfiguredDefaultModelStatus.Matched,
                configuredDefaultModel);
        }

        AvailableModel firstReturnedModel = context.AvailableModels[0];

        return new ModelSelectionDecision(
            firstReturnedModel.Id,
            ModelSelectionSource.FirstReturnedModel,
            configuredDefaultModel is null
                ? ConfiguredDefaultModelStatus.NotConfigured
                : ConfiguredDefaultModelStatus.NotFound,
            configuredDefaultModel);
    }

    private static string? ResolvePreferredModelId(
        IReadOnlyList<AvailableModel> availableModels,
        string? preferredModelId)
    {
        string? normalizedPreferredModelId = Normalize(preferredModelId);
        if (normalizedPreferredModelId is null)
        {
            return null;
        }

        foreach (AvailableModel availableModel in availableModels)
        {
            if (string.Equals(availableModel.Id, normalizedPreferredModelId, StringComparison.Ordinal))
            {
                return availableModel.Id;
            }
        }

        foreach (AvailableModel availableModel in availableModels)
        {
            if (HasMatchingTerminalSegment(availableModel.Id, normalizedPreferredModelId))
            {
                return availableModel.Id;
            }
        }

        return null;
    }

    private static bool HasMatchingTerminalSegment(string modelId, string preferredModelId)
    {
        int lastSlashIndex = modelId.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex == modelId.Length - 1)
        {
            return false;
        }

        return modelId[(lastSlashIndex + 1)..].Equals(preferredModelId, StringComparison.Ordinal);
    }

    private static string? Normalize(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
