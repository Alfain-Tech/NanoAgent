namespace FinalAgent.Application.Models;

public sealed record ReplCommandContext(
    string CommandName,
    string ArgumentText,
    IReadOnlyList<string> Arguments,
    string RawText,
    ReplSessionContext Session);
