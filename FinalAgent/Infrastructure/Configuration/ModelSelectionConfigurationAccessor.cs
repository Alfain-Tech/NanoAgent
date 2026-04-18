using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using Microsoft.Extensions.Options;

namespace FinalAgent.Infrastructure.Configuration;

internal sealed class ModelSelectionConfigurationAccessor : IModelSelectionConfigurationAccessor
{
    private readonly ApplicationOptions _options;

    public ModelSelectionConfigurationAccessor(IOptions<ApplicationOptions> options)
    {
        _options = options.Value;
    }

    public ModelSelectionSettings GetSettings()
    {
        string? configuredDefaultModel = Normalize(_options.Defaults.Model);
        string[] rankedPreferences = _options.ModelSelection.RankedPreferenceList
            .Select(Normalize)
            .Where(static value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new ModelSelectionSettings(
            configuredDefaultModel,
            rankedPreferences,
            TimeSpan.FromSeconds(_options.ModelSelection.CacheDurationSeconds));
    }

    private static string? Normalize(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
