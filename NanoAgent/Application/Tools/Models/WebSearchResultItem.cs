namespace NanoAgent.Application.Tools.Models;

public sealed record WebSearchResultItem(
    string Title,
    string Url,
    string? DisplayUrl,
    string? Snippet);
