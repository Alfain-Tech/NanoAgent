using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Tools.Models;

namespace NanoAgent.Infrastructure.Tools;

internal sealed partial class DuckDuckGoWebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;

    public DuckDuckGoWebSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WebSearchResult> SearchAsync(
        WebSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException(
                "A non-empty web search query must be provided.",
                nameof(request));
        }

        if (request.MaxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Web search max results must be greater than zero.");
        }

        using HttpRequestMessage httpRequest = new(
            HttpMethod.Get,
            BuildSearchUri(request.Query));

        using HttpResponseMessage response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        WebSearchResultItem[] results = ParseResults(html)
            .Take(request.MaxResults)
            .ToArray();

        return new WebSearchResult(
            request.Query,
            results);
    }

    private static Uri BuildSearchUri(string query)
    {
        return new($"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}", UriKind.Absolute);
    }

    private static IReadOnlyList<WebSearchResultItem> ParseResults(string html)
    {
        List<WebSearchResultItem> results = [];
        HashSet<string> seenUrls = new(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ResultRegex().Matches(html).Cast<Match>())
        {
            string url = NormalizeResultUrl(match.Groups["href"].Value);
            if (string.IsNullOrWhiteSpace(url) || !seenUrls.Add(url))
            {
                continue;
            }

            string title = CleanupHtmlText(match.Groups["title"].Value);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            string rest = match.Groups["rest"].Value;
            string? displayUrl = TryMatchGroup(ResultUrlRegex(), rest, "displayUrl");
            string? snippet = TryMatchGroup(ResultSnippetRegex(), rest, "snippet");

            results.Add(new WebSearchResultItem(
                title,
                url,
                string.IsNullOrWhiteSpace(displayUrl) ? null : displayUrl,
                string.IsNullOrWhiteSpace(snippet) ? null : snippet));
        }

        return results;
    }

    private static string? TryMatchGroup(
        Regex regex,
        string value,
        string groupName)
    {
        Match match = regex.Match(value);
        if (!match.Success)
        {
            return null;
        }

        return CleanupHtmlText(match.Groups[groupName].Value);
    }

    private static string CleanupHtmlText(string value)
    {
        string withoutTags = HtmlTagRegex().Replace(value, string.Empty);
        string decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    private static string NormalizeResultUrl(string rawHref)
    {
        string value = WebUtility.HtmlDecode(rawHref).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            value = "https:" + value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            return value;
        }

        if (!uri.Host.Contains("duckduckgo.com", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.AbsolutePath, "/l/", StringComparison.Ordinal))
        {
            return uri.ToString();
        }

        string? redirectedUrl = TryGetQueryParameter(uri.Query, "uddg");
        return string.IsNullOrWhiteSpace(redirectedUrl)
            ? uri.ToString()
            : redirectedUrl;
    }

    private static string? TryGetQueryParameter(
        string query,
        string key)
    {
        string trimmedQuery = query.TrimStart('?');
        if (trimmedQuery.Length == 0)
        {
            return null;
        }

        foreach (string pair in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = pair.IndexOf('=');
            string encodedName = separatorIndex >= 0
                ? pair[..separatorIndex]
                : pair;

            if (!string.Equals(
                    WebUtility.UrlDecode(encodedName),
                    key,
                    StringComparison.Ordinal))
            {
                continue;
            }

            string encodedValue = separatorIndex >= 0
                ? pair[(separatorIndex + 1)..]
                : string.Empty;

            return WebUtility.UrlDecode(encodedValue);
        }

        return null;
    }

    [GeneratedRegex(
        "<h2 class=\"result__title\">\\s*<a[^>]*class=\"result__a\"[^>]*href=\"(?<href>[^\"]+)\"[^>]*>(?<title>.*?)</a>\\s*</h2>(?<rest>.*?)(?=<h2 class=\"result__title\">|<div class=\"nav-link\">|</body>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ResultRegex();

    [GeneratedRegex(
        "<a class=\"result__url\"[^>]*>\\s*(?<displayUrl>.*?)\\s*</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ResultUrlRegex();

    [GeneratedRegex(
        "<a class=\"result__snippet\"[^>]*>(?<snippet>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ResultSnippetRegex();

    [GeneratedRegex(
        "<.*?>",
        RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(
        "\\s+",
        RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
