using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools;
using NanoAgent.Application.Tools.Models;
using FluentAssertions;
using Moq;

namespace NanoAgent.Tests.Application.Tools;

public sealed class WebSearchToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_QueryIsMissing()
    {
        WebSearchTool sut = new(Mock.Of<IWebSearchService>());

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("{}"),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Contain("requires a non-empty 'query'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_MaxResultsIsOutOfRange()
    {
        WebSearchTool sut = new(Mock.Of<IWebSearchService>());

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "query": "dotnet", "maxResults": 99 }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Contain("between 1 and 10");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnStructuredResults_When_QueryIsValid()
    {
        Mock<IWebSearchService> webSearchService = new(MockBehavior.Strict);
        webSearchService
            .Setup(service => service.SearchAsync(
                It.Is<WebSearchRequest>(request =>
                    request.Query == "dotnet" &&
                    request.MaxResults == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebSearchResult(
                "dotnet",
                [
                    new WebSearchResultItem(
                        ".NET documentation",
                        "https://learn.microsoft.com/en-us/dotnet/",
                        "learn.microsoft.com/en-us/dotnet/",
                        "Learn to use .NET.")
                ]));

        WebSearchTool sut = new(webSearchService.Object);

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "query": "dotnet", "maxResults": 3 }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.Success);
        result.JsonResult.Should().Contain("learn.microsoft.com");
        result.RenderPayload!.Text.Should().Contain(".NET documentation");
    }

    private static ToolExecutionContext CreateContext(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        return new ToolExecutionContext(
            "call_1",
            "web_search",
            document.RootElement.Clone(),
            TestSessionFactory.Create());
    }
}
