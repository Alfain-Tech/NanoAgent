namespace FinalAgent.Application.Models;

public sealed record SelectionPromptOption<T>(
    string Label,
    T Value,
    string? Description = null);
