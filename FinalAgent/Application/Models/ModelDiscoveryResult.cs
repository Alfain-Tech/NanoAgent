using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Models;

public sealed record ModelDiscoveryResult(
    IReadOnlyList<AvailableModel> AvailableModels,
    string SelectedModelId,
    ModelSelectionSource SelectionSource,
    ConfiguredDefaultModelStatus ConfiguredDefaultStatus,
    string? ConfiguredDefaultModel,
    bool HadDuplicateModelIds);
