namespace NanoAgent.Application.Tools.Models;

public sealed record WorkspaceFileSearchResult(
    string Query,
    string Path,
    IReadOnlyList<string> Matches);
