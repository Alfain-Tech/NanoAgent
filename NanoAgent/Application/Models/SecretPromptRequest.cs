namespace NanoAgent.Application.Models;

public sealed record SecretPromptRequest(
    string Label,
    string? Description = null,
    bool AllowCancellation = true);
