using FinalAgent.Application.Exceptions;
using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;

namespace FinalAgent.Domain.Services;

internal sealed class RankedModelSelectionPolicy : IModelSelectionPolicy
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
        HashSet<string> availableModels = context.AvailableModels
            .Select(model => model.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (configuredDefaultModel is not null && availableModels.Contains(configuredDefaultModel))
        {
            return new ModelSelectionDecision(
                configuredDefaultModel,
                ModelSelectionSource.ConfiguredDefault,
                ConfiguredDefaultModelStatus.Matched,
                configuredDefaultModel);
        }

        foreach (string rankedPreference in context.RankedPreferenceList)
        {
            string? normalizedPreference = Normalize(rankedPreference);
            if (normalizedPreference is not null && availableModels.Contains(normalizedPreference))
            {
                return new ModelSelectionDecision(
                    normalizedPreference,
                    ModelSelectionSource.RankedPreference,
                    configuredDefaultModel is null
                        ? ConfiguredDefaultModelStatus.NotConfigured
                        : ConfiguredDefaultModelStatus.NotFound,
                    configuredDefaultModel);
            }
        }

        string configuredModelMessage = configuredDefaultModel is null
            ? "No configured default model was provided."
            : $"Configured default model '{configuredDefaultModel}' was not returned by the provider.";

        throw new ModelSelectionException(
            $"{configuredModelMessage} None of the ranked preference models are available.");
    }

    private static string? Normalize(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
