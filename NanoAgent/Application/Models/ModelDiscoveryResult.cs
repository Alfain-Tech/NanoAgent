using NanoAgent.Domain.Models;

namespace NanoAgent.Application.Models;

public sealed record ModelDiscoveryResult(
    IReadOnlyList<AvailableModel> AvailableModels,
    string SelectedModelId,
    ModelSelectionSource SelectionSource,
    ConfiguredDefaultModelStatus ConfiguredDefaultStatus,
    string? ConfiguredDefaultModel,
    bool HadDuplicateModelIds);
