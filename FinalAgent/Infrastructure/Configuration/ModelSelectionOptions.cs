namespace FinalAgent.Infrastructure.Configuration;

public sealed class ModelSelectionOptions
{
    public int CacheDurationSeconds { get; set; } = 300;

    public List<string> RankedPreferenceList { get; set; } =
    [
        "gpt-5",
        "gpt-5-mini",
        "gpt-4.1",
        "gpt-4.1-mini"
    ];
}
