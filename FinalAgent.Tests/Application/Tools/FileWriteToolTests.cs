using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools;
using FinalAgent.Application.Tools.Models;
using FluentAssertions;
using Moq;

namespace FinalAgent.Tests.Application.Tools;

public sealed class FileWriteToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_ContentIsMissing()
    {
        FileWriteTool sut = new(Mock.Of<IWorkspaceFileService>());

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "path": "README.md" }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Contain("'content' string");
    }

    [Fact]
    public async Task ExecuteAsync_Should_WriteFile_When_ArgumentsAreValid()
    {
        Mock<IWorkspaceFileService> workspaceFileService = new(MockBehavior.Strict);
        workspaceFileService
            .Setup(service => service.WriteFileAsync(
                "README.md",
                "hello",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileWriteResult("README.md", false, 5));

        FileWriteTool sut = new(workspaceFileService.Object);

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "path": "README.md", "content": "hello", "overwrite": false }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.Success);
        result.JsonResult.Should().Contain("\"OverwroteExistingFile\":false");
        result.RenderPayload!.Title.Should().Contain("README.md");
    }

    private static ToolExecutionContext CreateContext(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        return new ToolExecutionContext(
            "call_1",
            "file_write",
            document.RootElement.Clone(),
            TestSessionFactory.Create());
    }
}
