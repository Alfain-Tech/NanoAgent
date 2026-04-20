namespace NanoAgent.Application.Tools.Models;

public sealed record WebSearchRequest(
    string Query,
    int MaxResults);
