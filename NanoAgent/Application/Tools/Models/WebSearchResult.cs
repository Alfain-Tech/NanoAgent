namespace NanoAgent.Application.Tools.Models;

public sealed record WebSearchResult(
    string Query,
    IReadOnlyList<WebSearchResultItem> Results);
