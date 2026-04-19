using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Models;

public sealed class ReplSessionContext
{
    private readonly HashSet<string> _availableModelIds;

    public ReplSessionContext(
        AgentProviderProfile providerProfile,
        string activeModelId,
        IReadOnlyList<string> availableModelIds)
    {
        ArgumentNullException.ThrowIfNull(providerProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeModelId);
        ArgumentNullException.ThrowIfNull(availableModelIds);

        ProviderProfile = providerProfile;
        AvailableModelIds = availableModelIds
            .Where(static modelId => !string.IsNullOrWhiteSpace(modelId))
            .Select(static modelId => modelId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (AvailableModelIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one available model must be provided.",
                nameof(availableModelIds));
        }

        _availableModelIds = new HashSet<string>(AvailableModelIds, StringComparer.Ordinal);

        string normalizedActiveModelId = activeModelId.Trim();
        if (!_availableModelIds.Contains(normalizedActiveModelId))
        {
            throw new ArgumentException(
                "The active model must exist in the available model set.",
                nameof(activeModelId));
        }

        ActiveModelId = normalizedActiveModelId;
    }

    public string ActiveModelId { get; private set; }

    public IReadOnlyList<string> AvailableModelIds { get; }

    public AgentProviderProfile ProviderProfile { get; }

    public string ProviderName => ProviderProfile.ProviderKind.ToDisplayName();

    public bool ContainsModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        return _availableModelIds.Contains(modelId.Trim());
    }

    public void SetActiveModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        string normalizedModelId = modelId.Trim();
        if (!_availableModelIds.Contains(normalizedModelId))
        {
            throw new InvalidOperationException(
                $"Model '{normalizedModelId}' is not available in the current session.");
        }

        ActiveModelId = normalizedModelId;
    }
}
