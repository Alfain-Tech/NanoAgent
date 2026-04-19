namespace NanoAgent.Application.Models;

public sealed record TextPromptRequest(
    string Label,
    string? Description = null,
    string? DefaultValue = null,
    bool AllowCancellation = true);
