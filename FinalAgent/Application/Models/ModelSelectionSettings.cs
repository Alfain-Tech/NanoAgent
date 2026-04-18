namespace FinalAgent.Application.Models;

public sealed record ModelSelectionSettings(
    string? ConfiguredDefaultModel,
    IReadOnlyList<string> RankedPreferenceList,
    TimeSpan CacheDuration);
