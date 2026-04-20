using NanoAgent.Application.Tools.Models;

namespace NanoAgent.Application.Abstractions;

public interface IWebSearchService
{
    Task<WebSearchResult> SearchAsync(
        WebSearchRequest request,
        CancellationToken cancellationToken);
}
