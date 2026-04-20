using System.Net;
using System.Net.Http;
using System.Text;
using NanoAgent.Application.Tools.Models;
using NanoAgent.Infrastructure.Tools;
using FluentAssertions;

namespace NanoAgent.Tests.Infrastructure.Tools;

public sealed class DuckDuckGoWebSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_RequestDuckDuckGoHtmlEndpoint_AndParseResults()
    {
        RecordingHandler handler = new(
            """
            <!DOCTYPE html>
            <html>
            <body>
              <div class="result results_links results_links_deep web-result ">
                <div class="links_main links_deep result__body">
                  <h2 class="result__title">
                    <a rel="nofollow" class="result__a" href="//duckduckgo.com/l/?uddg=https%3A%2F%2Flearn.microsoft.com%2Fen-us%2Fdotnet%2F">.NET documentation</a>
                  </h2>
                  <div class="result__extras">
                    <div class="result__extras__url">
                      <a class="result__url" href="//duckduckgo.com/l/?uddg=https%3A%2F%2Flearn.microsoft.com%2Fen-us%2Fdotnet%2F">
                        learn.microsoft.com/en-us/dotnet/
                      </a>
                    </div>
                  </div>
                  <a class="result__snippet" href="//duckduckgo.com/l/?uddg=https%3A%2F%2Flearn.microsoft.com%2Fen-us%2Fdotnet%2F">Learn to use <b>.NET</b> on any platform.</a>
                  <div class="clear"></div>
                </div>
              </div>
              <div class="result results_links results_links_deep web-result ">
                <div class="links_main links_deep result__body">
                  <h2 class="result__title">
                    <a rel="nofollow" class="result__a" href="https://dotnet.microsoft.com/">.NET home</a>
                  </h2>
                  <a class="result__snippet" href="https://dotnet.microsoft.com/">Official site.</a>
                  <div class="clear"></div>
                </div>
              </div>
              <div class="nav-link"></div>
            </body>
            </html>
            """);

        HttpClient httpClient = new(handler);
        DuckDuckGoWebSearchService sut = new(httpClient);

        WebSearchResult result = await sut.SearchAsync(
            new WebSearchRequest("dotnet", 2),
            CancellationToken.None);

        handler.RequestUri.Should().Be(new Uri("https://html.duckduckgo.com/html/?q=dotnet"));
        result.Query.Should().Be("dotnet");
        result.Results.Should().HaveCount(2);
        result.Results[0].Title.Should().Be(".NET documentation");
        result.Results[0].Url.Should().Be("https://learn.microsoft.com/en-us/dotnet/");
        result.Results[0].DisplayUrl.Should().Be("learn.microsoft.com/en-us/dotnet/");
        result.Results[0].Snippet.Should().Be("Learn to use .NET on any platform.");
        result.Results[1].Url.Should().Be("https://dotnet.microsoft.com/");
    }

    [Fact]
    public async Task SearchAsync_Should_RespectMaxResults()
    {
        RecordingHandler handler = new(
            """
            <html>
            <body>
              <h2 class="result__title">
                <a rel="nofollow" class="result__a" href="https://example.com/1">One</a>
              </h2>
              <a class="result__snippet" href="https://example.com/1">First.</a>
              <h2 class="result__title">
                <a rel="nofollow" class="result__a" href="https://example.com/2">Two</a>
              </h2>
              <a class="result__snippet" href="https://example.com/2">Second.</a>
              <div class="nav-link"></div>
            </body>
            </html>
            """);

        HttpClient httpClient = new(handler);
        DuckDuckGoWebSearchService sut = new(httpClient);

        WebSearchResult result = await sut.SearchAsync(
            new WebSearchRequest("example", 1),
            CancellationToken.None);

        result.Results.Should().ContainSingle();
        result.Results[0].Title.Should().Be("One");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public RecordingHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "text/html")
            };

            return Task.FromResult(response);
        }
    }
}
